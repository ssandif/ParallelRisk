using System.Collections.Generic;
using System.Collections.Immutable;

namespace ParallelRisk
{
    public readonly struct BoardState : IState<Move>
    {
        private readonly ImmutableArray<Continent> _continents;
        private readonly ImmutableAdjacencyMatrix _adjacency;
        private readonly ImmutableArray<Territory> _territories;

        public BoardState(ImmutableArray<Continent> continents, ImmutableAdjacencyMatrix adjacency, ImmutableArray<Territory> territories, bool maxPlayerTurn)
        {
            _continents = continents;
            _adjacency = adjacency;
            _territories = territories;
            IsMaxPlayerTurn = maxPlayerTurn;
        }

        public BoardState AttackUpdate(in Territory from, in Territory to)
        {
            var builder = _territories.ToBuilder();
            builder[from.Id] = from;
            builder[to.Id] = to;
            return new BoardState(_continents, _adjacency, builder.MoveToImmutable(), IsMaxPlayerTurn);
        }

        public BoardState PassTurn()
        {
            return new BoardState(_continents, _adjacency, _territories, !IsMaxPlayerTurn);
        }

        public int TotalContinentBonus(Player player)
        {
            int bonus = 0;
            foreach (Continent continent in _continents)
            {
                if (HasBonus(player, continent))
                    bonus += continent.Bonus;
            }
            return bonus;
        }

        public bool HasBonus(Player player, in Continent continent)
        {
            foreach(int id in continent.Territories)
            {
                if (_territories[id].Player != player)
                    return false;
            }

            return true;
        }

        public int TotalTerritoriesControlled(Player player)
        {
            int count = 0;
            foreach (Territory territory in _territories)
            {
                if (territory.Player == player)
                    ++count;
            }
            return count;
        }

        public double Heuristic()
        {
            const double C = 1;
            double value = 0;
            value += TotalTerritoriesControlled(Player.Max) - TotalTerritoriesControlled(Player.Min);
            value += C * (TotalContinentBonus(Player.Max) - TotalContinentBonus(Player.Min));
            return value;
        }

        public bool IsTerminal()
        {
            return TotalTerritoriesControlled(Player.Max) == 0 || TotalTerritoriesControlled(Player.Min) == 0;
        }

        public bool IsMaxPlayerTurn { get; }

        public IEnumerable<Move> Moves()
        {
            yield return Move.PassTurn(this);

            foreach (Territory from in _territories)
            {
                if (IsCurrentPlayer(from.Player))
                {
                    foreach (int tid in _adjacency.Adjacent(from.Id))
                    {
                        if (!IsCurrentPlayer(_territories[tid].Player))
                        {
                            yield return Move.Attack(this, from, _territories[tid]);
                        }
                    }
                }
            }
        }

        private bool IsCurrentPlayer(Player player)
        {
            return (IsMaxPlayerTurn && player == Player.Max) || (!IsMaxPlayerTurn && player == Player.Min);
        }
    }
}
