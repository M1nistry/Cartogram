using System;
using System.Collections.Generic;

namespace Cartogram
{
    public class Map : IEquatable<Map>
    {
        public int Id { get; set; }
        public int SqlId { get; set; }
        public string Rarity { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
        public int Quality { get; set; }
        public int Quantity { get; set; }
        public List<string> Affixes { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime FinishAt { get; set; }
        public Experience ExpBefore { get; set; }
        public Experience ExpAfter { get; set; }
        public string Notes { get; set; }
        public string League { get; set; }
        public string Character { get; set; }
        public bool OwnMap { get; set; }

        public bool Equals(Map other)
        {
            return other.Id == Id;
        }
    }

    public class Maps
    {
        public static string[] MapArray()
        {
            return new[]
            {
               "Academy", "Crypt","Dried Lake","Dunes","Dungeon","Grotto","Overgrown Ruin", "Tropical Island",
               "Arcade","Arsenal","Cemetery","Mountain Ledge","Sewer","Thicket", "Wharf","Ghetto",
               "Mud Geyser","Reef","Spider Lair","Springs","Vaal Pyramid", "Catacomb", "Overgrown Shrine",
               "Promenade","Shore","Spider Forest","Tunnel","Bog", "Coves", "Graveyard", "Pier",
               "Underground Sea","Arachnid Nest","Colonnade", "Dry Woods", "Strand", "Temple",
               "Jungle Valley", "Torture Chamber", "Waste Pool", "Mine", "Dry Peninsula", "Canyon",
               "Cells", "Dark Forest", "Gorge", "Maze", "Underground River", "Bazaar", "Necropolis",
               "Plateau", "Crematorium", "Precinct", "Shipyard", "Shrine", "Villa", "Palace", "Pit",
               "Desert", "Aqueduct", "Quarry", "Arena", "Abyss", "Village Ruin", "Wasteland", "Excavation",
               "Waterways", "Core", "Volcano", "Colosseum"
            };
        }
    }
}
