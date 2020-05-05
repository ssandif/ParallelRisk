using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelRisk
{
    // Generates a standard risk board.
    public static class Risk
    {
        // The id of the territory, safe to cast to and from int as necessary.
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
            GreatBritain,
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
            Afghanistan,
            Ural,
            MiddleEast
        }

        // Returns a randomized initial board state with the territories set up as a standard risk board. If seed is
        // null, uses a time-based random seed.
        public static BoardState RandomizedBoardPlacement(int? seed = null)
        {
            var random = (seed == null) ? new Random() : new Random(seed.GetValueOrDefault());

            return new BoardState(Continents(), AdjacencyMatrix(), ImmutableArray.CreateRange(RandomTerritories(random)), true);
        }

        // Use the provided random number generator to generate random troop placements on territories.
        private static IEnumerable<Territory> RandomTerritories(Random random)
        {
            return Enumerable.Range(0, 42).Select(id =>
            {
                // Set player to max, min, or neutral (0, 1, or 2, respectively)
                var player = (Player)random.Next(0, 3);
                // Add between 1 and 10 troops
                int troopCount = random.Next(1, 11);
                return new Territory(id, player, troopCount);
            });
        }

        // Returns the continents from the standard Risk board.
        private static ImmutableArray<Continent> Continents()
        {
            return ImmutableArray.Create(
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
                    Id.GreatBritain,
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
                    Id.Afghanistan,
                    Id.Ural,
                    Id.MiddleEast))
            );
        }

        // Returns an adjacency matrix representing the territory connections in the standard Risk board.
        private static ImmutableAdjacencyMatrix AdjacencyMatrix()
        {
            ImmutableAdjacencyMatrix.Builder builder = ImmutableAdjacencyMatrix.CreateBuilder(42);
            /*  Comment note: 
             *  // comments represent countries already attached to + the name of the current
             *  Multi line ones represent the current continent.
             */
            /* **North America** */
            // Alaska -> First
            AddConnection(builder, Id.Alaska, Id.Kamchatka);
            AddConnection(builder, Id.Alaska, Id.NorthwestTerritory);
            AddConnection(builder, Id.Alaska, Id.Alberta);
            // NW Terrority -> Hooked to Alaska
            AddConnection(builder, Id.NorthwestTerritory, Id.Greenland);
            AddConnection(builder, Id.NorthwestTerritory, Id.Ontario);
            AddConnection(builder, Id.NorthwestTerritory, Id.Alberta);
            // Alberta -> Hooked to NW Territory / Alaska
            AddConnection(builder, Id.Alberta, Id.Ontario);
            AddConnection(builder, Id.Alberta, Id.WesternUnitedStates);
            // Greenland -> Hooked to NW Territory/Alberta 
            AddConnection(builder, Id.Greenland, Id.Ontario);
            AddConnection(builder, Id.Greenland, Id.Quebec);
            AddConnection(builder, Id.Greenland, Id.Iceland);
            // Ontario -> Hooked to Greenland/Alberta/NW Territories
            AddConnection(builder, Id.Ontario, Id.Quebec);
            AddConnection(builder, Id.Ontario, Id.WesternUnitedStates);
            AddConnection(builder, Id.Ontario, Id.EasternUnitedStates);
            // Quebec -> Already hooked to Ontario/Greenland
            AddConnection(builder, Id.Quebec, Id.EasternUnitedStates);
            // Western US -> Hooked to Alberta/Ontario
            AddConnection(builder, Id.WesternUnitedStates, Id.EasternUnitedStates);
            AddConnection(builder, Id.WesternUnitedStates, Id.CentralAmerica);
            // Eastern US -> Hooked to Ontario / Quebec / Eastern US 
            AddConnection(builder, Id.EasternUnitedStates, Id.CentralAmerica);
            // Central America -> Hooked to USA
            AddConnection(builder, Id.CentralAmerica, Id.Venezuela);
            /* **South America** */
            // Venezuela -> Hooked to CAmerica
            AddConnection(builder, Id.Venezuela, Id.Brazil);
            AddConnection(builder, Id.Venezuela, Id.Peru);
            // Brazil -> Hooked to Venezuela
            AddConnection(builder, Id.Brazil, Id.Peru);
            AddConnection(builder, Id.Brazil, Id.Argentina);
            AddConnection(builder, Id.Brazil, Id.NorthAfrica);
            // Peru: Hooked to Venezuela/Brazil
            AddConnection(builder, Id.Peru, Id.Argentina);
            // Argentina: Hooked to all others
            /* **Europe** */
            // Iceland -> Hooked to Greenland
            AddConnection(builder, Id.Iceland, Id.GreatBritain);
            AddConnection(builder, Id.Iceland, Id.Scandinavia);
            // Scandinavia -> Hooked to Iceland
            AddConnection(builder, Id.Scandinavia, Id.Ukraine);
            AddConnection(builder, Id.Scandinavia, Id.GreatBritain);
            AddConnection(builder, Id.Scandinavia, Id.NorthernEurope);
            // Ukraine -> Hooked to Scandinavia
            AddConnection(builder, Id.Ukraine, Id.NorthernEurope);
            AddConnection(builder, Id.Ukraine, Id.SouthernEurope);
            AddConnection(builder, Id.Ukraine, Id.Ural);
            AddConnection(builder, Id.Ukraine, Id.Afghanistan);
            AddConnection(builder, Id.Ukraine, Id.MiddleEast);
            // Great Britain -> Hooked to Iceland / Scandinavia
            AddConnection(builder, Id.GreatBritain, Id.NorthernEurope);
            AddConnection(builder, Id.GreatBritain, Id.WesternEurope);
            // North Europe -> Hooked to GB, Ukraine, Scandinavia
            AddConnection(builder, Id.NorthernEurope, Id.SouthernEurope);
            AddConnection(builder, Id.NorthernEurope, Id.WesternEurope);
            // Western Europe -> Hooked to GB/Northern Europe
            AddConnection(builder, Id.WesternEurope, Id.SouthernEurope);
            AddConnection(builder, Id.WesternEurope, Id.NorthAfrica);
            // Southern Europe: Connected to all of Europe
            AddConnection(builder, Id.SouthernEurope, Id.NorthAfrica);
            AddConnection(builder, Id.SouthernEurope, Id.Egypt);
            AddConnection(builder, Id.SouthernEurope, Id.MiddleEast);
            /* **Africa** */
            // North Africa: Connected to other countries
            AddConnection(builder, Id.NorthAfrica, Id.Egypt);
            AddConnection(builder, Id.NorthAfrica, Id.EastAfrica);
            AddConnection(builder, Id.NorthAfrica, Id.Congo);
            // Egypt: Connected to NAfrica + Europe
            AddConnection(builder, Id.Egypt, Id.MiddleEast);
            AddConnection(builder, Id.Egypt, Id.EastAfrica);
            // East Africa: Connected to NAfrica / Egypt
            AddConnection(builder, Id.EastAfrica, Id.MiddleEast);
            AddConnection(builder, Id.EastAfrica, Id.Congo);
            AddConnection(builder, Id.EastAfrica, Id.Madagascar);
            AddConnection(builder, Id.EastAfrica, Id.SouthAfrica);
            // Congo: Connected to North/East Africa
            AddConnection(builder, Id.Congo, Id.SouthAfrica);
            // South Africa: Connected to all mainland Africa
            AddConnection(builder, Id.SouthAfrica, Id.Madagascar);
            // Madagascar: Already connected to South/East Africa
            /* **Asia** */
            // Ural: Connected to Ukraine already
            AddConnection(builder, Id.Ural, Id.Siberia);
            AddConnection(builder, Id.Ural, Id.Afghanistan);
            AddConnection(builder, Id.Ural, Id.China);
            // Siberia: Connected to Ural
            AddConnection(builder, Id.Siberia, Id.Yakutsk);
            AddConnection(builder, Id.Siberia, Id.Irkutsk);
            AddConnection(builder, Id.Siberia, Id.Mongolia);
            AddConnection(builder, Id.Siberia, Id.China);
            // Yakutsk: Connected to Siberia
            AddConnection(builder, Id.Yakutsk, Id.Kamchatka);
            AddConnection(builder, Id.Yakutsk, Id.Irkutsk);
            // Kamchatka: Connnected to Yakutsk / Alaska
            AddConnection(builder, Id.Kamchatka, Id.Irkutsk);
            AddConnection(builder, Id.Kamchatka, Id.Japan);
            AddConnection(builder, Id.Kamchatka, Id.Mongolia);
            // Irkutsk: Connected to Siberia/Yak/Kamchatka
            AddConnection(builder, Id.Irkutsk, Id.Mongolia);
            // Mongolia: Connected to Kamchat/Irk/Siberia
            AddConnection(builder, Id.Mongolia, Id.Japan);
            AddConnection(builder, Id.Mongolia, Id.China);
            // Afghanistan: connected to Ukraine
            AddConnection(builder, Id.Afghanistan, Id.China);
            AddConnection(builder, Id.Afghanistan, Id.India);
            AddConnection(builder, Id.Afghanistan, Id.MiddleEast);
            // China: Connected to Mognolia/Afgha
            AddConnection(builder, Id.China, Id.India);
            AddConnection(builder, Id.China, Id.Siam);
            // Middle East: Connected to Europe/Africa/Afgha
            AddConnection(builder, Id.MiddleEast, Id.India);
            // India: Connected to Afgha/China/ME
            AddConnection(builder, Id.India, Id.Siam);
            // Siam: Connected to Asia.
            AddConnection(builder, Id.Siam, Id.Indonesia);
            /* **Australia** */
            // Indonesia: Connected to Siam
            AddConnection(builder, Id.Indonesia, Id.NewGuinea);
            AddConnection(builder, Id.Indonesia, Id.WesternAustralia);
            // New Guinea: Connected to Indonesia
            AddConnection(builder, Id.NewGuinea, Id.EasternAustralia);
            AddConnection(builder, Id.NewGuinea, Id.WesternAustralia);
            // Western Australia: Connected to Indonesia/New Guinea
            AddConnection(builder, Id.WesternAustralia, Id.EasternAustralia);
            // Eastern Australia connecteed to New Guinea/Westenr Australia already.
            // All done initialization of connections!
            return builder.MoveToImmutable();
        }

        // Helper function for creating an immutable array of territories.
        private static ImmutableArray<int> Territories(params Id[] ids)
        {
            return ImmutableArray.CreateRange(ids.Select(x => (int)x));
        }

        // Helper function for adding a territory connection to the adjacency matrix. Makes casting to an integer
        // unnecessary.
        private static void AddConnection(ImmutableAdjacencyMatrix.Builder builder, Id t1, Id t2)
        {
            // Only need to set one way, the adjacency matrix assumes an undirected graph.
            builder[(int)t1, (int)t2] = true;
        }
    }
}
