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
            

            if (move.Action == Move.MoveType.Attack)
                Console.WriteLine($"Attack from {(Risk.Id)move.From.Id} to {(Risk.Id)move.To.Id}");
            else
                Console.WriteLine($"PassTurn");

            Move reinf = Minimax.SerialMove<BoardState, Move>(board, MaxDepth);

            Console.WriteLine($"{(Move.MoveType)reinf.Action}");
            if (reinf.Action == Move.MoveType.Reinforce) {
                if (reinf.From.Id != reinf.To.Id) {
                    Console.WriteLine($"Reinforce from {(Risk.Id)reinf.From.Id} to {(Risk.Id)reinf.To.Id}");
                } else {
                    Console.WriteLine($"Recommended no change.");
                }
            }
            
        }
    }
}
