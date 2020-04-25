using System;
using ParallelRisk;

namespace ParallelRiskConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            const int MaxDepth = 10;
            BoardState board = Risk.StandardBoard();
            Move move = Minimax.Serial<BoardState, Move>(board, MaxDepth);

            if (move.IsAttack)
                Console.WriteLine($"Attack from {(Risk.Id)move.From.Id} to {(Risk.Id)move.To.Id}");
            else
                Console.WriteLine("PassTurn");
        }
    }
}
