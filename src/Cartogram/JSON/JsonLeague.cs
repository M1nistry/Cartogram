using System;
using Newtonsoft.Json;

namespace Cartogram.JSON
{
    public class JsonLeague
    {
        [JsonProperty("shopForumID")]
        public string ForumId { get; set; }
        [JsonProperty("archivedLadder")]
        public int Archived { get; set; }
        [JsonProperty("shopForumURL")]
        public string ForumUrl { get; set; }
        [JsonProperty("active")]
        public bool Active { get; set; }
        [JsonProperty("itemjsonName")]
        public string ItemName { get; set; }
        [JsonProperty("shopURL")]
        public string ShopUrl { get; set; }
        [JsonProperty("apiName")]
        public string ApiName { get; set; }
        [JsonProperty("endTime")]
        public long EndTime { get; set; }
        [JsonProperty("league")]
        public string League { get; set; }
        [JsonProperty("startTime")]
        public long StartTime { get; set; }
        [JsonProperty("prettyName")]
        public string PrettyName { get; set; }
    }

    public class RootObject
    {
        public JsonLeague League { get; set; }
    }
}
