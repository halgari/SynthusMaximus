using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Armor
{
    public class ArmorSettings
    {
        [JsonProperty("armorFactorBody")]
        public float ArmorFactorBody { get; set; }

        [JsonProperty("armorFactorFeet")]
        public float ArmorFactorFeet { get; set; }

        [JsonProperty("armorFactorHands")]
        public float ArmorFactorHands { get; set; }

        [JsonProperty("armorFactorHead")]
        public float ArmorFactorHead { get; set; }

        [JsonProperty("armorFactorShield")]
        public float ArmorFactorShield { get; set; }

        [JsonProperty("meltdownOutputBody")]
        public int MeltdownOutputBody { get; set; }

        [JsonProperty("meltdownOutputFeet")]
        public int MeltdownOutputFeet { get; set; }

        [JsonProperty("meltdownOutputHands")]
        public int MeltdownOutputHands { get; set; }

        [JsonProperty("meltdownOutputHead")]
        public int MeltdownOutputHead { get; set; }

        [JsonProperty("meltdownOutputShield")]
        public int MeltdownOutputShield { get; set; }

        [JsonProperty("maxProtection")]
        public float MaxProtection { get; set; }

        [JsonProperty("protectionPerArmor")]
        public float ProtectionPerArmor { get; set; }

        [JsonProperty("armorRatingPCMax")]
        public float ArmorRatingPCMax { get; set; } 

        [JsonProperty("armorRatingMax")]
        public float ArmorRatingMax { get; set; }
    }
}