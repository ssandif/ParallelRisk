using System.Collections.Generic;

namespace ParallelRisk
{
    // This interface represents a game state that can be used by minimax. The type used by TMove should return IState
    // objects for its Outcome() function.
    public interface IState<TMove>
    {
        // The estimated utility of the current state.
        double Heuristic();

        // Whether or not this state represents the end of the game.
        bool IsTerminal();

        // True if it's the max player's turn, false if it's the min player's turn.
        bool IsMaxPlayerTurn { get; }

        // Returns all possible moves the current player may make.
        IEnumerable<TMove> Moves();
    }
}
