using System.Collections.Generic;

namespace ParallelRisk
{
    public interface IState<TMove>
    {
        double Heuristic();
        bool IsTerminal();
        bool IsMaxPlayerTurn { get; }
        IEnumerable<TMove> Moves();
    }
}
