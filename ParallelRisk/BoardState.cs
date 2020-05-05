using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using static System.Math;

namespace ParallelRisk
{
    // Represents the current state of the board, as well as basic board information such as connections between
    // territories and the continents being used. Note that this type is immutable, all functions that would normally
    // modify it return a new state instead.
    public readonly struct BoardState : IState<Move>
    {
        // The continents used on the board.
        public ImmutableArray<Continent> Continents { get; }

        // An adjacency matrix that represents connections between territories.
        public ImmutableAdjacencyMatrix Adjacency { get; }

        // All territories on the board, ordered such that the index of a territory is the same as its id.
        public ImmutableArray<Territory> Territories { get; }

        // True if it's the max player's turn, false if it's the min player's turn.
        public bool IsMaxPlayerTurn { get; }

        public BoardState(ImmutableArray<Continent> continents, ImmutableAdjacencyMatrix adjacency, ImmutableArray<Territory> territories, bool maxPlayerTurn)
        {
            Continents = continents;
            Adjacency = adjacency;
            Territories = territories;
            IsMaxPlayerTurn = maxPlayerTurn;
        }

        // Returns a new board state with the results of an attack.
        public BoardState AttackUpdate(in Territory from, in Territory to)
        {
            var builder = Territories.ToBuilder();
            builder[from.Id] = from;
            builder[to.Id] = to;
            return new BoardState(Continents, Adjacency, builder.MoveToImmutable(), IsMaxPlayerTurn);
        }

        // Returns a new board state with the turn passed to the next player.
        public BoardState Pass()
        {
            return new BoardState(Continents, Adjacency, Territories, !IsMaxPlayerTurn);
        }


        // Returns a new board state with the turn passed to the next player and "fortifyCount" troops moved from
        // the territory with fromId to the territory with toId.
        public BoardState PassAndFortify(int fromId, int toId, int fortifyCount)
        {
            var builder = Territories.ToBuilder();
            builder[fromId] = builder[fromId].ModifyTroops(-fortifyCount);
            builder[toId] = builder[toId].ModifyTroops(fortifyCount);
            return new BoardState(Continents, Adjacency, builder.MoveToImmutable(), !IsMaxPlayerTurn);
        }

        // Estimated utility for the current board state.
        public double Heuristic()
        {
            // note: this is the 'continent bonus' weighting
            const double C = 1;
            double value = 0;
            value += TotalTerritoriesControlled(Player.Max) - TotalTerritoriesControlled(Player.Min);
            value += C * (TotalContinentBonus(Player.Max) - TotalContinentBonus(Player.Min));
            return value;
        }

        // Returns true of the game is over (one of the non-neutral players has been eliminated).
        public bool IsTerminal()
        {
            return TotalTerritoriesControlled(Player.Max) == 0 || TotalTerritoriesControlled(Player.Min) == 0;
        }

        // Returns the total number of territories controlled by the player.
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

        // Returns the total contient bonus for the player.
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

        // Returns whether or not the player has the continent bonus for the specified continent.
        public bool HasBonus(Player player, in Continent continent)
        {
            foreach (int id in continent.Territories)
            {
                if (Territories[id].Player != player)
                    return false;
            }

            return true;
        }

        // Returns the total number of territories that can be attacked from territory "territoryId", assuming it's
        // controlled by the specified player.
        public int TotalAttackableTerritories(int territoryId, Player attacker)
        {
            int count = 0;
            foreach (int tid in Adjacency.Adjacent(territoryId))
            {
                if (Territories[tid].Player != attacker)
                    ++count;
            }
            return count;
        }

        // Returns all possible moves the current player may make (attack, fortify + pass, or pass).
        public IEnumerable<Move> Moves()
        {
            yield return Move.Pass(this);

            foreach (Move move in ReinforceMoves())
            {
                yield return move;
            }

            foreach (Territory from in Territories)
            {
                if (IsCurrentPlayer(from.Player) && from.TroopCount > 1)
                {
                    foreach (int tid in Adjacency.Adjacent(from.Id))
                    {
                        if (!IsCurrentPlayer(Territories[tid].Player))
                        {
                            yield return Move.Attack(this, from.Id, tid);
                        }
                    }
                }
            }
        }

        // All possible reinforcement moves.
        private IEnumerable<Move> ReinforceMoves()
        {
            var fromT = new List<Territory>();

            // build an expanded list of all potential move options
            foreach (Territory from in Territories)
            {
                if (IsCurrentPlayer(from.Player) && from.TroopCount > 1)
                {
                    foreach (int tid in Adjacency.Adjacent(from.Id))
                    {
                        if (!fromT.Contains(Territories[tid]) && IsCurrentPlayer(Territories[tid].Player))
                        {
                            fromT.Add(Territories[tid]);
                        }
                    }
                }
            }

            foreach (Territory ft in fromT)
            {
                var toT = new List<Territory>();
                AddAdjacentToList(toT, ft.Id);
                foreach (Territory to in toT) {
                    if (ft.Id == to.Id) {
                        continue;
                    }
                    yield return Move.PassAndFortify(this, ft.Id, to.Id, ft.TroopCount - 1);
                }
            }
        }

        private void AddAdjacentToList(List<Territory> to, int tid)
        {
            foreach (int tid2 in Adjacency.Adjacent(tid))
            {
                if (IsCurrentPlayer(Territories[tid2].Player) && !to.Contains(Territories[tid2]))
                {
                    // can optimize to only search if edge territory, but would be hard
                    // without optimizations in other parts of code to determine 'safe' territories
                    to.Add(Territories[tid2]);
                    // should check adjacent of these as well and add them, recursively...
                    // this gets *every* adjacent territory, but will stop if all of them are added
                    // it's basically depth first search
                    AddAdjacentToList(to, tid2);
                }
            }
            return;
        }


        // Returns the optimal number of occuplying troops for a successful invasion from territory "fromId" to
        // territory "toId"
        public int OptimalOccupyingTroops(int fromId, int toId, int attackers)
        {
            int available = Territories[fromId].TroopCount - 1;
            Player attacker = Territories[fromId].Player;
            // Subtract 1 for the territory you're about to occupy
            int fromAttacks = TotalAttackableTerritories(fromId, attacker) - 1;
            int toAttacks = TotalAttackableTerritories(toId, attacker);
            double ratio = (double)toAttacks / (fromAttacks + toAttacks);
            int optimal = (int)Round(available * ratio, MidpointRounding.AwayFromZero);
            return Min(attackers, optimal);
        }

        // Whether or not the specified player is the current player.
        private bool IsCurrentPlayer(Player player)
        {
            return (IsMaxPlayerTurn && player == Player.Max) || (!IsMaxPlayerTurn && player == Player.Min);
        }
    }
}
