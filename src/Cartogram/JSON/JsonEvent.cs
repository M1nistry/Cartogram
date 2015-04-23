using System.Collections.Generic;

namespace Cartogram.JSON
{
    public class JsonEvent
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public bool Event { get; set; }
        public string RegisterAt { get; set; }
        public string StartAt { get; set; }
        public string EndAt { get; set; }
        public List<Rule> Rules { get; set; }
        public List<object> Ladder { get; set; }
        public bool? TimedEvent { get; set; }
    }

    public class Rule
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
