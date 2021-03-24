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
        public ushort MeltdownOutputBody { get; set; }

        [JsonProperty("meltdownOutputFeet")]
        public ushort MeltdownOutputFeet { get; set; }

        [JsonProperty("meltdownOutputHands")]
        public ushort MeltdownOutputHands { get; set; }

        [JsonProperty("meltdownOutputHead")]
        public ushort MeltdownOutputHead { get; set; }

        [JsonProperty("meltdownOutputShield")]
        public ushort MeltdownOutputShield { get; set; }

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