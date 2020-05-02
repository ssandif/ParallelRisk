using System;
using ParallelRisk;

namespace ParallelRiskConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            int MaxDepth = 10;
            if (args.Length > 0)
            {
                int potentialDepth = int.Parse(args[0]);

                if (potentialDepth > 0)
                    MaxDepth = potentialDepth;
            }

            BoardState board = Risk.StandardBoard();
            Move move = Minimax.Serial<BoardState, Move>(board, MaxDepth);

            if (move.IsAttack)
                Console.WriteLine($"Attack from {(Risk.Id)move.FromId} to {(Risk.Id)move.ToId}");
            else
                Console.WriteLine("PassTurn");
        }
    }
}
