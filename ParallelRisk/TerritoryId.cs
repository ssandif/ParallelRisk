namespace ParallelRisk
{
    sealed class TerritoryId
    {
        public TerritoryId(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }
        public int Id { get; }
    }
}
