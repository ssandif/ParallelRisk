namespace ParallelRisk
{
    public readonly struct Territory
    {
        public Territory(int id, Player player, int troopCount)
        {
            Id = id;
            Player = player;
            TroopCount = troopCount;
        }

        public int Id { get; }
        public Player Player { get; }
        public int TroopCount { get; }

        public Territory ChangeControl(Player player, int troopCount)
        {
            return new Territory(Id, player, troopCount);
        }

        public Territory ModifyTroops(int amount)
        {
            return new Territory(Id, Player, TroopCount + amount);
        }
    }
}
