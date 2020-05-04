using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Math;


namespace ParallelRisk
{
    public static class AlphaBeta
    {
        public static TMove ParallelYbw<TState, TMove>(TState node, int depth, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        => ParallelYbw<TState, TMove>(node, depth, double.NegativeInfinity, double.PositiveInfinity, pool);

        public static TMove ParallelYbw<TState, TMove>(TState node, int depth, double alpha, double beta, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return default;

            var tasks = new List<(TMove Move, Task<double> Task)>();
            bool first = true;

            if (node.IsMaxPlayerTurn)
            {
                TMove bestMove = default;
                double value = double.NegativeInfinity;
                foreach (TMove move in node.Moves())
                {
                    if (first)
                    {
                        first = false;
                        bestMove = move;
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool);
                        alpha = Max(alpha, value);
                        if (alpha >= beta)
                            break;
                    }
                    else
                    {
                        ValueTask<double> task = pool.TryRun(() => ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));
                        if (task.IsCompleted)
                        {
                            if (task.Result > value)
                            {
                                bestMove = move;
                                value = task.Result;

                                alpha = Max(alpha, value);
                                if (alpha >= beta)
                                    break;
                            }
                        }
                        else
                        {
                            tasks.Add((move, task.AsTask()));
                        }
                    }
                }
                foreach ((TMove move, Task<double> task) in tasks)
                {
                    if (task.Result > value)
                    {
                        bestMove = move;
                        value = task.Result;

                        alpha = Max(alpha, value);
                        if (alpha >= beta)
                            break;
                    }
                }
                return bestMove;
            }
            else
            {
                TMove bestMove = default;
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                {
                    if (first)
                    {
                        first = false;
                        bestMove = move;
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool);

                        beta = Min(beta, value);
                        if (alpha >= beta)
                            break;
                    }
                    else
                    {
                        ValueTask<double> task = pool.TryRun(() => ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));
                        if (task.IsCompleted)
                        {
                            if (task.Result < value)
                            {
                                bestMove = move;
                                value = task.Result;

                                beta = Min(beta, value);
                                if (alpha >= beta)
                                    break;
                            }
                        }
                        else
                        {
                            tasks.Add((move, task.AsTask()));
                        }
                    }
                }
                foreach ((TMove move, Task<double> task) in tasks)
                {
                    if (task.Result < value)
                    {
                        bestMove = move;
                        value = task.Result;
                    }

                    beta = Min(beta, value);
                    if (alpha >= beta)
                        break;
                }
                return bestMove;
            }
        }

        private static double ParallelYbwAlphaBeta<TState, TMove>(TState node, int depth, double alpha, double beta, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return node.Heuristic();

            var tasks = new List<Task<double>>();
            bool first = true;

            if (node.IsMaxPlayerTurn)
            {
                double value = double.NegativeInfinity;
                foreach (TMove move in node.Moves())
                {
                    if (first)
                    {
                        first = false;
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool);

                        alpha = Max(alpha, value);
                        if (alpha >= beta)
                            break;
                    }
                    else
                    {
                        ValueTask<double> task = pool.TryRun(() => ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));
                        if (task.IsCompleted)
                        {
                            value = Max(value, task.Result);

                            alpha = Max(alpha, value);
                            if (alpha >= beta)
                                break;
                        }
                        else
                        {
                            tasks.Add(task.AsTask());
                        }
                    }
                }
                foreach (Task<double> task in tasks)
                {
                    value = Max(value, task.Result);

                    alpha = Max(alpha, value);
                    if (alpha >= beta)
                        break;
                }
                return value;
            }
            else
            {
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                {
                    if (first)
                    {
                        first = false;
                        value = Min(value, ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));

                        beta = Min(beta, value);
                        if (alpha >= beta)
                            break;
                    }
                    else
                    {
                        ValueTask<double> task = pool.TryRun(() => ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));
                        if (task.IsCompleted)
                        {
                            value = Min(value, task.Result);

                            beta = Min(beta, value);
                            if (alpha >= beta)
                                break;
                        }
                        else
                        {
                            tasks.Add(task.AsTask());
                        }
                    }
                }
                foreach (Task<double> task in tasks)
                {
                    value = Min(value, task.Result);

                    beta = Min(beta, value);
                    if (alpha >= beta)
                        break;
                }
                return value;
            }
        }

        private static double ParallelYbwEstimatedOutcome<TState, TMove>(TMove node, int depth, double alpha, double beta, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            var tasks = new List<Task<double>>();
            bool first = true;
            double value = 0;
            foreach ((double probability, TState outcome) in node.Outcomes())
            {
                if (first)
                {
                    first = false;
                    value += probability * ParallelYbwAlphaBeta<TState, TMove>(outcome, depth - 1, alpha, beta, pool);
                }
                else
                {
                    ValueTask<double> task = pool.TryRun(() => probability * ParallelYbwAlphaBeta<TState, TMove>(outcome, depth - 1, alpha, beta, pool));
                    if (task.IsCompleted)
                        value += task.Result;
                    else
                        tasks.Add(task.AsTask());
                }
            }
            foreach (Task<double> task in tasks)
            {
                value += task.Result;
            }
            return value;
        }

        public static TMove ParallelYbwc<TState, TMove>(TState node, int depth, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        => ParallelYbwc<TState, TMove>(node, depth, double.NegativeInfinity, double.PositiveInfinity, pool);

        public static TMove ParallelYbwc<TState, TMove>(TState node, int depth, double alpha, double beta, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return default;

            using var source = new CancellationTokenSource();
            CancellationToken sourceToken = source.Token;
            var localLock = new object();
            var tasks = new List<Task>();
            bool first = true;

            if (node.IsMaxPlayerTurn)
            {
                TMove bestMove = default;
                double value = double.NegativeInfinity;
                foreach (TMove move in node.Moves())
                {
                    if (first)
                    {
                        first = false;
                        bestMove = move;
                        value = ParallelYbwcEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool, CancellationToken.None).GetValueOrDefault();

                        alpha = Max(alpha, value);
                        if (alpha >= beta)
                            break;
                    }
                    else
                    {
                        if (source.IsCancellationRequested)
                            break;

                        ValueTask task = pool.TryRun(() =>
                        {
                            double? v = ParallelYbwcEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool, sourceToken);
                            if (v != null)
                            {
                                lock (localLock)
                                {
                                    if (v > value)
                                    {
                                        bestMove = move;
                                        value = v.GetValueOrDefault();
                                    }

                                    alpha = Max(alpha, value);
                                    if (alpha >= beta)
                                    {
                                        source.Cancel();
                                    }
                                }
                            }
                        });

                        if (!task.IsCompleted)
                        {
                            tasks.Add(task.AsTask());
                        }
                    }
                }

                Task.WhenAll(tasks).Wait();

                return bestMove;
            }
            else
            {
                TMove bestMove = default;
                double value = double.NegativeInfinity;
                foreach (TMove move in node.Moves())
                {
                    if (first)
                    {
                        first = false;
                        bestMove = move;
                        value = ParallelYbwcEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool, CancellationToken.None).GetValueOrDefault();

                        alpha = Max(alpha, value);
                        if (alpha >= beta)
                            break;
                    }
                    else
                    {
                        if (source.IsCancellationRequested)
                            break;

                        ValueTask task = pool.TryRun(() =>
                        {
                            double? v = ParallelYbwcEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool, sourceToken);
                            if (v != null)
                            {
                                lock (localLock)
                                {
                                    if (v < value)
                                    {
                                        bestMove = move;
                                        value = v.GetValueOrDefault();
                                    }

                                    beta = Min(beta, value);
                                    if (alpha >= beta)
                                    {
                                        source.Cancel();
                                    }
                                }
                            }
                        });

                        if (!task.IsCompleted)
                        {
                            tasks.Add(task.AsTask());
                        }
                    }
                }

                Task.WhenAll(tasks).Wait();

                return bestMove;
            }
        }

        private static double ParallelYbwcAlphaBeta<TState, TMove>(TState node, int depth, double alpha, double beta, ControlledThreadPool pool, CancellationToken token)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return node.Heuristic();

            using var source = new CancellationTokenSource();
            CancellationToken sourceToken = source.Token;
            var localLock = new object();
            var tasks = new List<Task>();
            bool first = true;

            if (node.IsMaxPlayerTurn)
            {
                double value = double.NegativeInfinity;
                foreach (TMove move in node.Moves())
                {
                    if (token.IsCancellationRequested)
                    {
                        source.Cancel();
                        return 0;
                    }

                    if (first)
                    {
                        first = false;
                        double? v = ParallelYbwcEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool, sourceToken);

                        if (v != null)
                        {
                            value = v.GetValueOrDefault();
                            alpha = Max(alpha, value);
                            if (alpha >= beta)
                                break;
                        }
                    }
                    else
                    {
                        if (source.IsCancellationRequested)
                            break;

                        ValueTask task = pool.TryRun(() =>
                        {
                            double? v = ParallelYbwcEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool, sourceToken);
                            if (v != null)
                            {
                                lock (localLock)
                                {
                                    value = Max(value, v.GetValueOrDefault());
                                    alpha = Max(alpha, value);
                                    if (alpha >= beta)
                                    {
                                        source.Cancel();
                                    }
                                }
                            }
                        });

                        if (!task.IsCompleted)
                        {
                            tasks.Add(task.AsTask());
                        }
                    }
                }

                try
                {
                    Task.WhenAll(tasks).Wait(token);
                } catch { }
                source.Cancel();
                return value;
            }
            else
            {
                double value = double.NegativeInfinity;
                foreach (TMove move in node.Moves())
                {
                    if (token.IsCancellationRequested)
                    {
                        source.Cancel();
                        return value;
                    }

                    if (first)
                    {
                        first = false;
                        double? v = ParallelYbwcEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool, sourceToken);

                        if (v != null)
                        {
                            value = v.GetValueOrDefault();
                            alpha = Max(alpha, value);
                            if (alpha >= beta)
                                break;
                        }
                    }
                    else
                    {
                        if (source.IsCancellationRequested)
                            break;

                        ValueTask task = pool.TryRun(() =>
                        {
                            double? v = ParallelYbwcEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool, sourceToken);
                            if (v != null)
                            {
                                lock (localLock)
                                {
                                    value = Min(value, v.GetValueOrDefault());
                                    beta = Min(beta, value);
                                    if (alpha >= beta)
                                    {
                                        source.Cancel();
                                    }
                                }
                            }
                        });

                        if (!task.IsCompleted)
                        {
                            tasks.Add(task.AsTask());
                        }
                    }
                }

                try
                {
                    Task.WhenAll(tasks).Wait(token);
                } catch { }
                source.Cancel();
                return value;
            }
        }

        private static double? ParallelYbwcEstimatedOutcome<TState, TMove>(TMove node, int depth, double alpha, double beta, ControlledThreadPool pool, CancellationToken token)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            var tasks = new List<Task<double>>();
            bool first = true;
            double value = 0;
            foreach ((double probability, TState outcome) in node.Outcomes())
            {
                if (first)
                {
                    first = false;
                    value += probability * ParallelYbwcAlphaBeta<TState, TMove>(outcome, depth - 1, alpha, beta, pool, token);
                }
                else
                {
                    ValueTask<double> task = pool.TryRun(() => probability * ParallelYbwcAlphaBeta<TState, TMove>(outcome, depth - 1, alpha, beta, pool, token));
                    if (task.IsCompleted)
                        value += task.Result;
                    else
                        tasks.Add(task.AsTask());
                }
            }
            foreach (Task<double> task in tasks)
            {
                value += task.Result;
            }
            return token.IsCancellationRequested ? (double?)null : value;
        }

        public static TMove Parallel<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        => Parallel<TState, TMove>(node, depth, double.NegativeInfinity, double.PositiveInfinity);

        private static TMove Parallel<TState, TMove>(TState node, int depth, double alpha, double beta)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return default;

            var taskList = new List<(TMove Move, Task<double> Task)>();
            if (node.IsMaxPlayerTurn)
            {
                TMove bestMove = default;
                double value = double.NegativeInfinity;
                foreach (TMove move in node.Moves())
                {
                    taskList.Add((move, Task.Run(() => SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta))));
                }
                Console.WriteLine($"Added {taskList.Count} processes.");
                foreach ((TMove move, Task<double> task) in taskList)
                {
                    if (task.Result > value)
                    {
                        bestMove = move;
                        value = task.Result;
                    }
                }
                return bestMove;
            }
            else
            {
                TMove bestMove = default;
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                {
                    taskList.Add((move, Task.Run(() => SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta))));
                }
                Console.WriteLine($"Added {taskList.Count} processes.");
                foreach ((TMove move, Task<double> task) in taskList)
                {
                    if (task.Result < value)
                    {
                        bestMove = move;
                        value = task.Result;
                    }
                }
                return bestMove;
            }
        }

        public static TMove Serial<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        => Serial<TState, TMove>(node, depth, double.NegativeInfinity, double.PositiveInfinity);

        private static TMove Serial<TState, TMove>(TState node, int depth, double alpha, double beta)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return default;

            if (node.IsMaxPlayerTurn)
            {
                (TMove Move, double Utility) value = (default, double.NegativeInfinity);
                foreach (TMove move in node.Moves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta);
                    if (newUtil > value.Utility)
                        value = (move, newUtil);

                    alpha = Max(alpha, value.Utility);
                    if (alpha >= beta)
                        break;
                }
                return value.Move;
            }
            else
            {
                (TMove Move, double Utility) value = (default, double.PositiveInfinity);
                foreach (TMove move in node.Moves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta);
                    if (newUtil < value.Utility)
                        value = (move, newUtil);

                    beta = Min(beta, value.Utility);
                    if (alpha >= beta)
                        break;
                }
                return value.Move;
            }
        }

        private static double SerialAlphaBeta<TState, TMove>(TState node, int depth, double alpha, double beta)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return node.Heuristic();

            if (node.IsMaxPlayerTurn)
            {
                double value = double.NegativeInfinity;
                foreach (TMove atk in node.Moves())
                {
                    value = Max(value, SerialEstimatedOutcome<TState, TMove>(atk, depth, alpha, beta));

                    alpha = Max(alpha, value);
                    if (alpha >= beta)
                        break;
                }
                return value;
            }
            else
            {
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                {
                    value = Min(value, SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta));

                    beta = Min(beta, value);
                    if (alpha >= beta)
                        break;
                }
                return value;
            }
        }

        private static double SerialEstimatedOutcome<TState, TMove>(TMove node, int depth, double alpha, double beta)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            double value = 0;
            foreach ((double probability, TState outcome) in node.Outcomes())
            {
                value += probability * SerialAlphaBeta<TState, TMove>(outcome, depth - 1, alpha, beta);
            }
            return value;
        }
    }
}
