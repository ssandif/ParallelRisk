using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Math;


namespace ParallelRisk
{
    public static class Minimax
    {
        // Parallel implementation of minimax using the Young Brothers Wait concept. Returns the recommended move.
        public static TMove ParallelYbw<TState, TMove>(TState node, int depth, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return default;

            // Tasks assigned to other threads and their corresponding moves
            var tasks = new List<(TMove Move, Task<double> Task)>();

            // Tracks whether the first node is being accessed
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
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool);
                    }
                    else
                    {
                        ValueTask<double> task = pool.RunOnPoolIfPossible(() =>
                            ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool));

                        // If completed, its result can be directly immediately, otherwise store it for later use
                        if (task.IsCompleted)
                        {
                            if (task.Result > value)
                            {
                                bestMove = move;
                                value = task.Result;
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
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool);
                    }
                    else
                    {
                        ValueTask<double> task = pool.RunOnPoolIfPossible(() =>
                            ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool));

                        // If completed, its result can be directly immediately, otherwise store it for later use
                        if (task.IsCompleted)
                        {
                            if (task.Result < value)
                            {
                                bestMove = move;
                                value = task.Result;
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
                }

                return bestMove;
            }
        }

        // Parallel Young Brothers Wait: Returns utility instead of move (unlike the top-level function).
        private static double ParallelYbwMinimax<TState, TMove>(TState node, int depth, ControlledThreadPool pool)
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
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool);
                    }
                    else
                    {
                        ValueTask<double> task = pool.RunOnPoolIfPossible(() =>
                            ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool));

                        // If completed, its result can be directly immediately, otherwise store it for later use
                        if (task.IsCompleted)
                            value = Max(value, task.Result);
                        else
                            tasks.Add(task.AsTask());
                    }
                }

                // Wait for each task to complete and update the maximum
                foreach (Task<double> task in tasks)
                {
                    value = Max(value, task.Result);
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
                        value = Min(value, ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool));
                    }
                    else
                    {
                        ValueTask<double> task = pool.RunOnPoolIfPossible(() =>
                            ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool));

                        // If completed, its result can be accessed immediately, otherwise store it for later use
                        if (task.IsCompleted)
                            value = Min(value, task.Result);
                        else
                            tasks.Add(task.AsTask());
                    }
                }

                // Wait for each task to complete and update the minimum
                foreach (Task<double> task in tasks)
                {
                    value = Min(value, task.Result);
                }

                return value;
            }
        }

        // Parallel Young Brothers Wait: Returns utility of the move, based on sum of outcomee utilities weighted by
        // probability.
        private static double ParallelYbwEstimatedOutcome<TState, TMove>(TMove node, int depth, ControlledThreadPool pool)
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
                    value += probability * ParallelYbwMinimax<TState, TMove>(outcome, depth - 1, pool);
                }
                else
                {
                    ValueTask<double> task = pool.RunOnPoolIfPossible(() => 
                        probability * ParallelYbwMinimax<TState, TMove>(outcome, depth - 1, pool));
                    
                    // If completed, it didn't run on the pool, and its result can be directly added to "value",
                    // otherwise, store it so the task can work in the background.
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

        // Parellel implementation of minimax that only parallelizes the top level. Returns the recommended move.
        public static TMove Parallel<TState, TMove>(TState node, int depth)
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
                    taskList.Add((move, Task.Run(() => SerialEstimatedOutcome<TState, TMove>(move, depth))));
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
                    taskList.Add((move, Task.Run(() => SerialEstimatedOutcome<TState, TMove>(move, depth))));
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

        // Serial implementation of minimax. Returns the recommended move.
        public static TMove Serial<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return default;

            if (node.IsMaxPlayerTurn)
            {
                // Choose maximum child, storing the move that corresponds to the utility
                TMove bestMove = default;
                double value = double.NegativeInfinity;
                foreach (TMove move in node.Moves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth);
                    if (newUtil > value)
                    {
                        bestMove = move;
                        value = newUtil;
                    }
                }
                return bestMove;
            }
            else
            {
                // Choose minimum child, storing the move that corresponds to the utility
                TMove bestMove = default;
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth);
                    if (newUtil < value)
                    {
                        bestMove = move;
                        value = newUtil;
                    }
                }
                return bestMove;
            }

        }

        // Serial: Returns utility instead of move (unlike the top-level function).
        private static double SerialMinimax<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return node.Heuristic();

            if (node.IsMaxPlayerTurn)
            {
                // Choose maximum child
                double value = double.NegativeInfinity;
                foreach (TMove atk in node.Moves())
                    value = Max(value, SerialEstimatedOutcome<TState, TMove>(atk, depth));
                return value;
            }
            else
            {
                // Choose minimum child
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                    value = Min(value, SerialEstimatedOutcome<TState, TMove>(move, depth));
                return value;
            }
        }


        // Serial: Returns utility of the move, based on sum of outcomee utilities weighted by probability.
        private static double SerialEstimatedOutcome<TState, TMove>(TMove node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            double value = 0;
            foreach ((double probability, TState outcome) in node.Outcomes())
            {
                value += probability * SerialMinimax<TState, TMove>(outcome, depth - 1);
            }
            return value;
        }
    }
}
