using System.Collections.Generic;

namespace ParallelRisk
{
    // Represents a move taken on a game state of type TState.
    public interface IMove<TState>
    {
        // Returns tuples of the possible outcomes that may occur from the move and the probability of each outcome.
        // For moves with no randomness, simply return a single outcome with a probability of 1. Probabilties are
        // represented as out of 1 (1 = 100%, 0.75 = 75%, etc.).
        IEnumerable<(double Probability, TState Outcome)> Outcomes();
    }
}
