using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
﻿using static System.Math;


namespace ParallelRisk
{
    public static class AlphaBeta
    {
        public static TMove ParallelYbw<TState, TMove>(TState node, int depth, ControlledThreadPool pool)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        => ParallelYbw<TState, TMove>(node, depth, double.NegativeInfinity, double.PositiveInfinity, pool);

        private static TMove ParallelYbw<TState, TMove>(TState node, int depth, double alpha, double beta, ControlledThreadPool pool)
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
                    double newUtil = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool);
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
                    double newUtil = ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool);
                    if (newUtil < value.Utility)
                        value = (move, newUtil);

                    beta = Min(beta, value.Utility);
                    if (alpha >= beta)
                        break;
                }
                return value.Move;
            }
        }
        private static double ParallelYbwAlphaBeta<TState, TMove>(TState node, int depth, double alpha, double beta, ControlledThreadPool pool)
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
                    value = Max(value, ParallelYbwEstimatedOutcome<TState, TMove>(atk, depth, alpha, beta, pool));

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
                    value = Min(value, ParallelYbwEstimatedOutcome<TState, TMove>(move, depth, alpha, beta, pool));

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
            return node.Outcomes()
                .AsParallel()
                .Select(o => o.Probability * ParallelYbwAlphaBeta<TState, TMove>(o.Outcome, depth - 1, alpha, beta, pool))
                .Sum();
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

            var taskList = new List<Task>();
            int i = 0;
            if (node.IsMaxPlayerTurn)
            {
                (TMove Move, double Utility) value = (default, double.NegativeInfinity);
                foreach (TMove move in node.Moves())
                {
                    i++;
                    taskList.Add( Task.Run(() => {
                        double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta);
                        if (newUtil > value.Utility)
                            value = (move, newUtil);

                        alpha = Max(alpha, value.Utility);
                        // Need to fix this somehow
                        //if (alpha >= beta)
                        //    break;
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
                        double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta);
                        if (newUtil < value.Utility)
                            value = (move, newUtil);

                        beta = Min(beta, value.Utility);
                        // How to fix this...
                        //if (alpha >= beta)
                        //    break;
                    }));
                }
                Console.WriteLine($"Added {i} processes.");
                Task.WaitAll(taskList.ToArray());
                return value.Move;
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
