using System;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelRisk
{
    public static class Risk
    {
        public enum Id : int
        {
            Alaska,
            NorthwestTerritory,
            Greenland,
            Alberta,
            Ontario,
            Quebec,
            WesternUnitedStates,
            EasternUnitedStates,
            CentralAmerica,
            Venezuela,
            Peru,
            Brazil,
            Argentina,
            NorthAfrica,
            Egypt,
            EastAfrica,
            Congo,
            SouthAfrica,
            Madagascar,
            Iceland,
            Scandinavia,
            Ukraine,
            GreatBritian,
            NorthernEurope,
            SouthernEurope,
            WesternEurope,
            Indonesia,
            NewGuinea,
            WesternAustralia,
            EasternAustralia,
            Siam,
            India,
            China,
            Mongolia,
            Japan,
            Irkutsk,
            Yakutsk,
            Kamchatka,
            Siberia,
            Afgha,
            Ural,
            MiddleEast
        }

        private static ImmutableArray<int> Territories(params Id[] ids)
        {
            return ImmutableArray.CreateRange(ids.Select(x => (int)x));
        }

        public static BoardState StandardBoard()
        {
            var random = new Random(0);
            var continents = ImmutableArray.Create(
                new Continent("North America", 5, Territories(
                    Id.Alaska,
                    Id.NorthwestTerritory,
                    Id.Greenland,
                    Id.Alberta,
                    Id.Ontario,
                    Id.Quebec,
                    Id.WesternUnitedStates,
                    Id.EasternUnitedStates,
                    Id.CentralAmerica)),
                new Continent("South America", 2, Territories(
                    Id.Venezuela,
                    Id.Peru,
                    Id.Brazil,
                    Id.Argentina)),
                new Continent("Africa", 3, Territories(
                    Id.NorthAfrica,
                    Id.Egypt,
                    Id.EastAfrica,
                    Id.Congo,
                    Id.SouthAfrica,
                    Id.Madagascar)),
                new Continent("Europe", 5, Territories(
                    Id.Iceland,
                    Id.Scandinavia,
                    Id.Ukraine,
                    Id.GreatBritian,
                    Id.NorthernEurope,
                    Id.SouthernEurope,
                    Id.WesternEurope)),
                new Continent("Australia", 2, Territories(
                    Id.Indonesia,
                    Id.NewGuinea,
                    Id.WesternAustralia,
                    Id.EasternAustralia)),
                new Continent("Asia", 7, Territories(
                    Id.Siam,
                    Id.India,
                    Id.China,
                    Id.Mongolia,
                    Id.Japan,
                    Id.Irkutsk,
                    Id.Yakutsk,
                    Id.Kamchatka,
                    Id.Siberia,
                    Id.Afgha,
                    Id.Ural,
                    Id.MiddleEast))

            );
            ImmutableAdjacencyMatrix.Builder builder = ImmutableAdjacencyMatrix.CreateBuilder(42);
            AddConnection(builder, Id.Alaska, Id.NorthwestTerritory);
            AddConnection(builder, Id.Alaska, Id.Alberta);
            AddConnection(builder, Id.NorthwestTerritory, Id.Greenland);
            AddConnection(builder, Id.NorthwestTerritory, Id.Ontario);
            AddConnection(builder, Id.NorthwestTerritory, Id.Alberta);
            AddConnection(builder, Id.Alberta, Id.Ontario);
            AddConnection(builder, Id.Alberta, Id.WesternUnitedStates);
            AddConnection(builder, Id.Greenland, Id.Ontario);
            AddConnection(builder, Id.Greenland, Id.Quebec);
            var territories = Enumerable.Range(0, 42).Select(id =>
            {
                var player = (Player)random.Next(0, 3);
                int troopCount = random.Next(1, 11);
                return new Territory(id, player, troopCount);
            }).ToList();
            return new BoardState(continents, builder.MoveToImmutable(), ImmutableArray.CreateRange(territories), true);
        }

        private static void AddConnection(ImmutableAdjacencyMatrix.Builder builder, Id t1, Id t2)
        {
            builder[(int)t1, (int)t2] = true;
        }
    }
}
