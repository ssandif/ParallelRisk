﻿using System;
using System.Diagnostics;
using System.Threading;
using CommandLine;
using ParallelRisk;

namespace ParallelRiskConsole
{
    class Program
    {
        class Options
        {
            [Option('d', "maxdepth", Required = false, Default = 10, HelpText = "Maximum depth to search to.")]
            public int MaxDepth { get; set; }

            [Option('a', "alphabeta", Required = false, Default = false, HelpText = "Whether or not to use alpha-beta pruning.")]
            public bool AlphaBeta { get; set; }

            [Option('P', "parallel", Required = false, Default = false, HelpText = "Use the MPI-enabled version of the algorithm.")]
            public bool Parallel { get; set; }

            [Option('t', "time", Required = false, Default = false, HelpText = "Times the program runtime.")]
            public bool Time { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(MainWithOptions);
        }

        static void MainWithOptions(Options options)
        {
            Stopwatch stopwatch = null;

            BoardState board = Risk.StandardBoard();
            var pool = new ControlledThreadPool(32);

            if (options.Time)
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }

            Move move = (options.AlphaBeta, options.Parallel) switch
            {
                (true, true) => AlphaBeta.Parallel<BoardState, Move>(board, options.MaxDepth),
                (true, false) => AlphaBeta.Serial<BoardState, Move>(board, options.MaxDepth),
                (false, true) => Minimax.Parallel<BoardState, Move>(board, options.MaxDepth),
                (false, false) => Minimax.Serial<BoardState, Move>(board, options.MaxDepth)
            };

            stopwatch?.Stop();

            if (move.IsAttack)
                Console.WriteLine($"Attack from {(Risk.Id)move.FromId} to {(Risk.Id)move.ToId}");
            else if (move.FortifyCount > 0)
                Console.WriteLine($"Pass turn and fortify {move.FortifyCount} troops from {(Risk.Id)move.FromId} to {(Risk.Id)move.ToId}");
            else
                Console.WriteLine("Pass turn and don't fortify.");

            if (options.Time)
                Console.WriteLine($"Runtime (s): {stopwatch.Elapsed.TotalSeconds}");
        }
    }
}
