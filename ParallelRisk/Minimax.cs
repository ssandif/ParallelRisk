using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

            if (node.IsMaxPlayerTurn)
            {
                TMove firstMove = node.Moves().First();
                double firstValue = ParallelYbwEstimatedOutcome<TState, TMove>(firstMove, depth, pool);
                (TMove restMove, double restUtility) = Task.WhenAll(node.Moves()
                    .Skip(1)
                    .Select(move => pool.TryRun(() => (Move: move, Utility: ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool)))))
                    .Result
                    .Aggregate((x, y) => x.Utility > y.Utility ? x : y);
                return firstValue > restUtility ? firstMove : restMove;
            }
            else
            {
                TMove firstMove = node.Moves().First();
                double firstValue = ParallelYbwEstimatedOutcome<TState, TMove>(firstMove, depth, pool);
                (TMove restMove, double restUtility) = Task.WhenAll(node.Moves()
                    .Skip(1)
                    .Select(move => pool.TryRun(() => (Move: move, Utility: ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool)))))
                    .Result
                    .Aggregate((x, y) => x.Utility < y.Utility ? x : y);
                return firstValue < restUtility ? firstMove : restMove;
            }
        }

        private static double ParallelYbwMinimax<TState, TMove>(TState node, int depth, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return node.Heuristic();

            if (node.IsMaxPlayerTurn)
            {
                double first = ParallelYbwEstimatedOutcome<TState, TMove>(node.Moves().First(), depth, pool);
                double rest = Task.WhenAll(node.Moves()
                    .Skip(1)
                    .Select(move => pool.TryRun(() => ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool))))
                    .Result
                    .Max();
                return Max(first, rest);
            }
            else
            {
                double first = ParallelYbwEstimatedOutcome<TState, TMove>(node.Moves().First(), depth, pool);
                double rest = Task.WhenAll(node.Moves()
                    .Skip(1)
                    .Select(move => pool.TryRun(() => ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, pool))))
                    .Result
                    .Min();
                return Min(first, rest);
            }
        }

        private static double ParallelYbwEstimatedOutcome<TState, TMove>(TMove node, int depth, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            (double probability, TState outcome) = node.Outcomes().First();
            double value = probability * ParallelYbwMinimax<TState, TMove>(outcome, depth - 1, pool);
            ThreadPool.GetAvailableThreads(out int workerThreads, out _);
            value += Task.WhenAll(node.Outcomes()
                .Skip(1)
                .Select(o => pool.TryRun(() => o.Probability * ParallelYbwMinimax<TState, TMove>(o.Outcome, depth - 1, pool))))
                .Result
                .Sum();
            return value;
        }

        public static TMove Serial<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState> {
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

            List<Task> taskList = new List<Task>();

            int i = 0;
            if (node.IsMaxPlayerTurn)
            {
                
                (TMove Move, double Utility) value = (default, double.NegativeInfinity);
                foreach (TMove move in node.Moves())
                {
                    i++;
                    taskList.Add( Task.Run(() => {
                        double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth);
                        if (newUtil > value.Utility)
                            value = (move, newUtil);
                    }));
                }
                Console.WriteLine($"Added {i} processes.");
                Task.WaitAll(taskList.ToArray());
                return value.Move;

            }
            else
            {
                (TMove Move, double Utility) value = (default, double.PositiveInfinity);
                foreach (TMove move in node.Moves())
                {
                    i++;
                    taskList.Add( Task.Run(() => {
                        double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth);
                        if (newUtil < value.Utility)
                            value = (move, newUtil);
                    }));
                }
                Console.WriteLine($"Added {i} processes.");
                Task.WaitAll(taskList.ToArray());
                return value.Move;
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
