using System;
using ParallelRisk;

namespace ParallelRiskConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            int MaxDepth = 10;
            if (args.Length > 0) {
                int potentialDepth = Int32.Parse(args[0]);
                if (potentialDepth > 0) MaxDepth = potentialDepth; 
            }
            BoardState board = Risk.StandardBoard();
            Move move = Minimax.Serial<BoardState, Move>(board, MaxDepth);

            if (move.IsAttack)
                Console.WriteLine($"Attack from {(Risk.Id)move.From.Id} to {(Risk.Id)move.To.Id}");
            else
                Console.WriteLine("PassTurn");
        }
    }
}
