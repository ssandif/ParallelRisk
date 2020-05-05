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
        // Parallel implementation of minimax with alpha-beta pruning using the Young Brothers Wait concept. Returns
        // the recommended move.
        public static TMove ParallelYbw<TState, TMove>(TState node, int depth, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        => ParallelYbw<TState, TMove>(node, depth, double.NegativeInfinity, double.PositiveInfinity, pool);

        // Parallel Young Brothers Wait: Called by the above function with the proper values. Returns the recommended
        // move.
        public static TMove ParallelYbw<TState, TMove>(TState node, int depth, double alpha, double beta,
                ControlledThreadPool pool)
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
                    // Young brothers wait: always execute the first node serially.
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
                        ValueTask<double> task = pool.RunOnPoolIfPossible(() =>
                            ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));

                        // If completed, its result can be directly immediately, otherwise store it for later use
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

                // Wait for each task to complete and update the best move
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
                    // Young brothers wait: always execute the first node serially.
                    if (first)
                    {
                        first = false;
                        bestMove = move;
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool);

                        beta = Min(beta, value);
                        if (alpha >= beta)
                            break; // Pruning
                    }
                    else
                    {
                        ValueTask<double> task = pool.RunOnPoolIfPossible(() =>
                            ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));

                        // If completed, its result can be directly immediately, otherwise store it for later use
                        if (task.IsCompleted)
                        {
                            if (task.Result < value)
                            {
                                bestMove = move;
                                value = task.Result;

                                beta = Min(beta, value);
                                if (alpha >= beta)
                                    break; // Pruning
                            }
                        }
                        else
                        {
                            tasks.Add((move, task.AsTask()));
                        }
                    }
                }

                // Wait for each task to complete and update the best move
                foreach ((TMove move, Task<double> task) in tasks)
                {
                    if (task.Result < value)
                    {
                        bestMove = move;
                        value = task.Result;
                    }

                    beta = Min(beta, value);
                    if (alpha >= beta)
                        break; // Pruning, no need to wait for other tasks
                }

                return bestMove;
            }
        }

        // Parallel Young Brothers Wait: Returns utility instead of move (unlike the top-level function).
        private static double ParallelYbwAlphaBeta<TState, TMove>(TState node, int depth, double alpha, double beta,
                ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return node.Heuristic();

            // Tasks assigned to other threads
            var tasks = new List<Task<double>>();

            // Tracks whether the first node is being accessed
            bool first = true;

            if (node.IsMaxPlayerTurn)
            {
                double value = double.NegativeInfinity;
                foreach (TMove move in node.Moves())
                {
                    // Young brothers wait: always execute the first node serially.
                    if (first)
                    {
                        first = false;
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool);

                        alpha = Max(alpha, value);
                        if (alpha >= beta)
                            break; // Pruning
                    }
                    else
                    {
                        ValueTask<double> task = pool.RunOnPoolIfPossible(() =>
                            ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));

                        // If completed, its result can be accessed immediately, otherwise store it for later use
                        if (task.IsCompleted)
                        {
                            value = Max(value, task.Result);

                            alpha = Max(alpha, value);
                            if (alpha >= beta)
                                break; // Pruning
                        }
                        else
                        {
                            tasks.Add(task.AsTask());
                        }
                    }
                }

                // Wait for each task to complete and update the maximum
                foreach (Task<double> task in tasks)
                {
                    value = Max(value, task.Result);

                    alpha = Max(alpha, value);
                    if (alpha >= beta)
                        break; // Pruning, no need to wait for other tasks
                }

                return value;
            }
            else
            {
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                {
                    // Young brothers wait: always execute the first node serially.
                    if (first)
                    {
                        first = false;
                        value = Min(value, ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));

                        beta = Min(beta, value);
                        if (alpha >= beta)
                            break; // Pruning
                    }
                    else
                    {
                        ValueTask<double> task = pool.RunOnPoolIfPossible(() =>
                            ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));

                        // If completed, its result can be accessed immediately, otherwise store it for later use
                        if (task.IsCompleted)
                        {
                            value = Min(value, task.Result);

                            beta = Min(beta, value);
                            if (alpha >= beta)
                                break; // Pruning
                        }
                        else
                        {
                            tasks.Add(task.AsTask());
                        }
                    }
                }

                // Wait for each task to complete and update the minimum
                foreach (Task<double> task in tasks)
                {
                    value = Min(value, task.Result);

                    beta = Min(beta, value);
                    if (alpha >= beta)
                        break; // Pruning, no need to wait for other tasks
                }

                return value;
            }
        }

        // Parallel Young Brothers Wait: Returns utility of the move, based on sum of outcomee utilities weighted by
        // probability.
        private static double ParallelYbwEstimatedOutcome<TState, TMove>(TMove node, int depth, double alpha,
                double beta, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            // Tasks assigned to other threads
            var tasks = new List<Task<double>>();

            // Tracks whether the first node is being accessed
            bool first = true;

            // Accumulates the utility
            double value = 0;

            foreach ((double probability, TState outcome) in node.Outcomes())
            {
                // Young brothers wait: always execute the first node serially.
                if (first)
                {
                    first = false;
                    value += probability * ParallelYbwAlphaBeta<TState, TMove>(outcome, depth - 1, alpha, beta, pool);
                }
                else
                {
                    ValueTask<double> task = pool.RunOnPoolIfPossible(() =>
                        probability * ParallelYbwAlphaBeta<TState, TMove>(outcome, depth - 1, alpha, beta, pool));

                    // If completed, its result can be accessed immediately, otherwise store it for later use
                    if (task.IsCompleted)
                        value += task.Result;
                    else
                        tasks.Add(task.AsTask());
                }
            }

            // Wait for each task to complete and add its result to "value"
            foreach (Task<double> task in tasks)
            {
                value += task.Result;
            }

            return value;
        }

        // Parellel implementation of minimax with alpha-beta pruning that only parallelizes the top level. Returns the
        // recommended move.
        public static TMove Parallel<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        => Parallel<TState, TMove>(node, depth, double.NegativeInfinity, double.PositiveInfinity);

        // Parallel: Called by the above function with the proper starting parameters. Returns the recommended move.
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
                    taskList.Add((move, Task.Run(() =>
                        SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta))));
                }
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
                    taskList.Add((move, Task.Run(() =>
                        SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta))));
                }
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

        // Serial implementation of minimax with alpha-beta pruning.
        public static TMove Serial<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        => Serial<TState, TMove>(node, depth, double.NegativeInfinity, double.PositiveInfinity);

        // Serial: Called by the above function with the proper starting parameters. Returns the recommended move.
        private static TMove Serial<TState, TMove>(TState node, int depth, double alpha, double beta)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return default;

            if (node.IsMaxPlayerTurn)
            {
                TMove bestMove = default;
                double value = double.NegativeInfinity;
                foreach (TMove move in node.Moves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta);
                    if (newUtil > value)
                    {
                        bestMove = move;
                        value = newUtil;
                    }

                    alpha = Max(alpha, value);
                    if (alpha >= beta)
                        break; // Pruning
                }
                return bestMove;
            }
            else
            {
                TMove bestMove = default;
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta);
                    if (newUtil < value)
                    {
                        bestMove = move;
                        value = newUtil;
                    }

                    beta = Min(beta, value);
                    if (alpha >= beta)
                        break; // Pruning
                }
                return bestMove;
            }
        }

        // Serial: Returns utility instead of move (unlike the top-level function).
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
                        break; // Pruning
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
                        break; // Pruning
                }
                return value;
            }
        }

        // Serial: Returns utility of the move, based on sum of outcomee utilities weighted by probability.
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
