namespace ParallelRisk
{
    // Represents a territory on the board. Note that this type is immutable, all functions return a new territory
    // struct.
    public readonly struct Territory
    {
        public Territory(int id, Player player, int troopCount)
        {
            Id = id;
            Player = player;
            TroopCount = troopCount;
        }

        // The index of the territory in BoardState.Territories, acts as a unique identifier.
        public int Id { get; }

        // The player who controls the territory.
        public Player Player { get; }

        // The number of troops in the territory. Should always be at least 1.
        public int TroopCount { get; }

        // Returns a modified version of the territory, giving the specified player control and setting the number of
        // troops present.
        public Territory ChangeControl(Player player, int troopCount)
        {
            return new Territory(Id, player, troopCount);
        }

        // Returns a modified version of the territory, adding the number of troops specified, or subtracting that many
        // if the argument is negative.
        public Territory ModifyTroops(int amount)
        {
            return new Territory(Id, Player, TroopCount + amount);
        }
    }
}
