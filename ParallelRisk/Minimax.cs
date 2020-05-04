using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Math;


namespace ParallelRisk
{
    public static class Minimax
    {
        public static TMove ParallelYbw<TState, TMove>(TState node, int depth, ControlledThreadPool pool)
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
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool);
                    }
                    else
                    {
                        ValueTask<double> task = pool.TryRun(() => ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool));
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
                    if (first)
                    {
                        first = false;
                        bestMove = move;
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool);
                    }
                    else
                    {
                        ValueTask<double> task = pool.TryRun(() => ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool));
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

        private static double ParallelYbwMinimax<TState, TMove>(TState node, int depth, ControlledThreadPool pool)
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
                        value = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool);
                    }
                    else
                    {
                        ValueTask<double> task = pool.TryRun(() => ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool));
                        if (task.IsCompleted)
                            value = Max(value, task.Result);
                        else
                            tasks.Add(task.AsTask());
                    }
                }
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
                    if (first)
                    {
                        first = false;
                        value = Min(value, ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool));
                    }
                    else
                    {
                        ValueTask<double> task = pool.TryRun(() => ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool));
                        if (task.IsCompleted)
                            value = Min(value, task.Result);
                        else
                            tasks.Add(task.AsTask());
                    }
                }
                foreach (Task<double> task in tasks)
                {
                    value = Min(value, task.Result);
                }
                return value;
            }
        }

        private static double ParallelYbwEstimatedOutcome<TState, TMove>(TMove node, int depth, ControlledThreadPool pool)
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
                    value += probability * ParallelYbwMinimax<TState, TMove>(outcome, depth - 1, pool);
                }
                else
                {
                    ValueTask<double> task = pool.TryRun(() => probability * ParallelYbwMinimax<TState, TMove>(outcome, depth - 1, pool));
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

        public static TMove Serial<TState, TMove>(TState node, int depth)
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
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth);
                    if (newUtil > value.Utility)
                        value = (move, newUtil);
                }
                return value.Move;
            }
            else
            {
                (TMove Move, double Utility) value = (default, double.PositiveInfinity);
                foreach (TMove move in node.Moves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth);
                    if (newUtil < value.Utility)
                        value = (move, newUtil);
                }
                return value.Move;
            }

        }

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
                    taskList.Add((move, Task.Run(() => SerialEstimatedOutcome<TState, TMove>(move, depth))));
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

        private static double SerialMinimax<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return node.Heuristic();

            if (node.IsMaxPlayerTurn)
            {
                double value = double.NegativeInfinity;
                foreach (TMove atk in node.Moves())
                    value = Max(value, SerialEstimatedOutcome<TState, TMove>(atk, depth));
                return value;
            }
            else
            {
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                    value = Min(value, SerialEstimatedOutcome<TState, TMove>(move, depth));
                return value;
            }
        }

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
