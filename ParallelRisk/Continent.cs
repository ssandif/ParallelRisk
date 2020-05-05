using System.Collections.Immutable;

namespace ParallelRisk
{
    // Represents a continent on the board. Note that this type is read only.
    public readonly struct Continent
    {
        // The name of the continent.
        public string Name { get; }

        // The continent bonus gained by controlling all territories in the continent.
        public int Bonus { get; }

        // The territories that make up the continent.
        public ImmutableArray<int> Territories { get; }

        public Continent(string name, int bonus, ImmutableArray<int> territories)
        {
            Name = name;
            Bonus = bonus;
            Territories = territories;
        }
    }
}