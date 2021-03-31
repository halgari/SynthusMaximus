using System;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Ammunition
{
    public class AmmunitionModifier
    {
        [JsonProperty("identifier")] public string Identifier { get; set; } = "";
        [JsonProperty("damageModifier")] public float DamageModifier { get; set; }
        [JsonProperty("rangeModifier")] public float RangeModifier { get; set; }
        [JsonProperty("speedModifier")] public float SpeedModifier { get; set; }
        [JsonProperty("gravityModifier")] public float GravityModifier { get; set; }
        [JsonProperty("nameSubstrings")] public string[] NameSubstrings { get; set; } = Array.Empty<string>();
    }
}