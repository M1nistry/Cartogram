using System;
using System.Collections.Generic;

namespace Cartogram
{
    class Map : IEquatable<Map>
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

        public bool Equals(Map other)
        {
            return other.Id == Id;
        }
    }
}
