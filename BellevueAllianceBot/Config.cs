using Newtonsoft.Json;
using System.IO;

namespace BellevueAllianceBot
{
    public class Config
    {
        public const string Path = "Config.json";
        
        public static readonly Config Instance = Create();

        [JsonProperty("ba_id")]
        public ulong BADiscordID { get; set; }

        public Config()
        {
        }

        private static Config Create()
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path))!;
        }
    }
}
