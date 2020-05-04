using System;
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

            [Option('P', "parallel", SetName = "parallelism", Required = false, Default = false, HelpText = "Use the default parallel implementation.")]
            public bool Parallel { get; set; }

            [Option('y', "ybw", SetName = "parallelism", Required = false, Default = false, HelpText = "Use the Young Brother Waits parallel implementation.")]
            public bool YoungBrotherWaits { get; set; }

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
            var pool = new ControlledThreadPool(8);

            options.AlphaBeta = true;
            options.MaxDepth = 4;
            options.YoungBrotherWaits = true;
            options.Time = true;

            if (options.Time)
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }

            Move move = (options.AlphaBeta, options.Parallel, options.YoungBrotherWaits) switch
            {
                (true, false, true) => AlphaBeta.ParallelYbwc<BoardState, Move>(board, options.MaxDepth, pool),
                (true, true, false) => AlphaBeta.Parallel<BoardState, Move>(board, options.MaxDepth),
                (true, false, false) => AlphaBeta.Serial<BoardState, Move>(board, options.MaxDepth),
                (false, false, true) => Minimax.ParallelYbw<BoardState, Move>(board, options.MaxDepth, pool),
                (false, true, false) => Minimax.Parallel<BoardState, Move>(board, options.MaxDepth),
                (false, false, false) => Minimax.Serial<BoardState, Move>(board, options.MaxDepth),
                _ => throw new ArgumentException()
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
