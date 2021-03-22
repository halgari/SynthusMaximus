using Newtonsoft.Json;

namespace SynthusMaximus.Data.LowLevel
{
    public class GeneralSettings
    {

        [JsonProperty("outputLanguage")] public string OutputLanguage { get; set; } = "ENGLISH";

        [JsonProperty("useMage")] 
        public bool UseMage { get; set; } 

        [JsonProperty("useThief")] 
        public bool UseThief { get; set; } 

        [JsonProperty("useWarrior")] 
        public bool UseWarrior { get; set; } 

        [JsonProperty("removeUnspecificStartingSpells")] 
        public bool RemoveUnspecificStartingSpells { get; set; }
    }

}