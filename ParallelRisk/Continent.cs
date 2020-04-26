using System.Collections.Immutable;

namespace ParallelRisk
{
    public readonly struct Continent
    {
        public string Name { get; }
        public int Bonus { get; }
        public ImmutableArray<int> Territories { get; }

        public Continent(string name, int bonus, ImmutableArray<int> territories)
        {
            Name = name;
            Bonus = bonus;
            Territories = territories;
        }
    }
}