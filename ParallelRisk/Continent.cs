using System.Collections.Generic;
using System.Collections.Immutable;

namespace ParallelRisk
{
    public readonly struct Continent
    {
        public string Name { get; }
        public int Bonus { get; }

        public IEnumerable<int> Territories => _territories;

        private readonly ImmutableArray<int> _territories;

        public Continent(string name, int bonus, ImmutableArray<int> territories)
        {
            Name = name;
            Bonus = bonus;
            _territories = territories;
        }
    }
}