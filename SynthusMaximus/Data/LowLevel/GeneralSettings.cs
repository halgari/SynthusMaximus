using Mutagen.Bethesda;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.LowLevel
{
    public class GeneralSettings
    {

        [JsonProperty("outputLanguage")] public Language OutputLanguage { get; set; } = Language.English;

        [JsonProperty("useMage")] public bool UseMage { get; set; } = true;

        [JsonProperty("useThief")] public bool UseThief { get; set; } = true;

        [JsonProperty("useWarrior")] public bool UseWarrior { get; set; } = true;

        [JsonProperty("removeUnspecificStartingSpells")]
        public bool RemoveUnspecificStartingSpells { get; set; } = true;
    }

}