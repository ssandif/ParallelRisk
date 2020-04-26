using System.Collections.Generic;
using System.Collections.Immutable;

namespace ParallelRisk
{
    public readonly struct BoardState : IState<Move>
    {
        public ImmutableArray<Continent> Continents { get; }
        public ImmutableAdjacencyMatrix Adjacency { get; }
        public ImmutableArray<Territory> Territories { get; }

        public BoardState(ImmutableArray<Continent> continents, ImmutableAdjacencyMatrix adjacency, ImmutableArray<Territory> territories, bool maxPlayerTurn)
        {
            Continents = continents;
            Adjacency = adjacency;
            Territories = territories;
            IsMaxPlayerTurn = maxPlayerTurn;
        }

        public BoardState AttackUpdate(in Territory from, in Territory to)
        {
            var builder = Territories.ToBuilder();
            builder[from.Id] = from;
            builder[to.Id] = to;
            return new BoardState(Continents, Adjacency, builder.MoveToImmutable(), IsMaxPlayerTurn);
        }

        public BoardState PassTurn()
        {
            return new BoardState(Continents, Adjacency, Territories, !IsMaxPlayerTurn);
        }

        public int TotalContinentBonus(Player player)
        {
            int bonus = 0;
            foreach (Continent continent in Continents)
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
                if (Territories[id].Player != player)
                    return false;
            }

            return true;
        }

        public int TotalTerritoriesControlled(Player player)
        {
            int count = 0;
            foreach (Territory territory in Territories)
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

            foreach (Territory from in Territories)
            {
                if (IsCurrentPlayer(from.Player) && from.TroopCount > 1)
                {
                    foreach (int tid in Adjacency.Adjacent(from.Id))
                    {
                        if (!IsCurrentPlayer(Territories[tid].Player))
                        {
                            yield return Move.Attack(this, from, Territories[tid]);
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
