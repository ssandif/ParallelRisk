using System;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelRisk
{
    public class ControlledThreadPool
    {
        private readonly object _lock = new object();
        private int _threads;

        public ControlledThreadPool(int threads = 1)
        {
            _threads = threads;
        }

        public Task<T> TryRun<T>(Func<T> function)
        {
            if (_threads > 0)
            {
                lock (_lock)
                {
                    if (_threads > 0)
                    {
                        --_threads;
                        return Task.Run(function).ContinueWith(ReturnThread);
                    }
                }
            }

            return Task.FromResult(function());
        }

        public Task<T> TryRun<T>(Func<T> function, CancellationToken token)
        {
            if (_threads > 0)
            {
                lock (_lock)
                {
                    if (_threads > 0)
                    {
                        --_threads;
                        return Task.Run(function, token).ContinueWith(ReturnThread);
                    }
                }
            }

            return Task.FromResult(function());
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