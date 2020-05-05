using System;
using System.Threading.Tasks;

namespace ParallelRisk
{
    // Provides a controlled way to run tasks such that, when possible, they are run on background workers up to the
    // limit imposed by the pool. Note that the current implementation uses Tasks from the default ThreadPool.
    public sealed class ControlledThreadPool
    {
        // Lock used to synchronize access to _workerThreads
        private readonly object _lock = new object();

        // The number of worker threads. Should never be below 0.
        private int _workerThreads;

        // Sets the number of worker threads. Keep in mind that the total number of threads used by RunOnPoolIfPossible
        // will be one more than this number, because you must consider the manager thread. the function is called
        // from.
        public ControlledThreadPool(int workerThreads)
        {
            _workerThreads = workerThreads;
        }

        // Runs the function on a task from the pool if possible, otherwise, runs it on the current thread. Note that
        // the task will technically be subject to the behavior of the default ThreadPool, so be careful using tasks
        // elsewhere if you want to guarantee good parallel performance.
        public ValueTask<T> RunOnPoolIfPossible<T>(Func<T> function)
        {
            // Check if worker threads are available. This early check avoids excess locking
            if (_workerThreads > 0)
            {
                lock (_lock)
                {
                    // Check again to ensure number hasn't changed after the lock
                    if (_workerThreads > 0)
                    {
                        --_workerThreads;
                        // Task must be continued with ReturnThread in order to ensure the thread returns to the pool
                        return new ValueTask<T>(Task.Run(function).ContinueWith(ReturnThread));
                    }
                }
            }

            // Run function on the current thread and return the result.
            return new ValueTask<T>(function());
        }

        // Runs the action on a task from the pool if possible, otherwise, runs it on the current thread. Note that
        // the task will technically be subject to the behavior of the default ThreadPool, so be careful using tasks
        // elsewhere if you want to guarantee good parallel performance.
        public ValueTask RunOnPoolIfPossible(Action action)
        {
            // Check if worker threads are available. This early check avoids excess locking
            if (_workerThreads > 0)
            {
                lock (_lock)
                {
                    // Check again to ensure number hasn't changed after the lock
                    if (_workerThreads > 0)
                    {
                        --_workerThreads;
                        // Task must be continued with ReturnThread in order to ensure the thread returns to the pool
                        return new ValueTask(Task.Run(action).ContinueWith(ReturnThread));
                    }
                }
            }

            // Run function on the current thread and return
            action();
            return new ValueTask();
        }

        // Return one thread to the thread pool.
        private void ReturnThread(Task task)
        {
            lock (_lock)
            {
                ++_workerThreads;
            }
        }

        // Return one thread to the thread pool.
        private T ReturnThread<T>(Task<T> task)
        {
            lock (_lock)
            {
                ++_workerThreads;
            }
            return task.Result;
        }
    }
}