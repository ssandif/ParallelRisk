using System;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelRisk
{
    public sealed class ControlledThreadPool
    {
        private readonly object _lock = new object();
        private int _threads;

        public ControlledThreadPool(int threads = 1)
        {
            _threads = threads;
        }

        public ValueTask<T> TryRun<T>(Func<T> function)
        {
            if (_threads > 0)
            {
                lock (_lock)
                {
                    if (_threads > 0)
                    {
                        --_threads;
                        return new ValueTask<T>(Task.Run(function).ContinueWith(ReturnThread));
                    }
                }
            }

            return new ValueTask<T>(function());
        }

        public ValueTask TryRun(Action action)
        {
            if (_threads > 0)
            {
                lock (_lock)
                {
                    if (_threads > 0)
                    {
                        --_threads;
                        return new ValueTask(Task.Run(action).ContinueWith(ReturnThread));
                    }
                }
            }

            action();
            return new ValueTask();
        }

        private void ReturnThread(Task task)
        {
            lock (_lock)
            {
                ++_threads;
            }
        }

        private T ReturnThread<T>(Task<T> task)
        {
            lock (_lock)
            {
                ++_threads;
            }
            return task.Result;
        }
    }
}