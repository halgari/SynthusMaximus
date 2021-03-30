using System;
using Newtonsoft.Json;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.DTOs.Ammunition
{
    public class AmmunitionType
    {
        [JsonProperty("identifier")] public string Identifier { get; set; } = "";
        [JsonProperty("type")] public BaseAmmunitionType Type { get; set; } = BaseAmmunitionType.Arrow;
        [JsonProperty("damageBase")] public float DamageBase { get; set; }
        [JsonProperty("rangeBase")] public float RangeBase { get; set; }
        [JsonProperty("speedBase")] public float SpeedBase { get; set; }
        [JsonProperty("gravityBase")] public float GravityBase { get; set; }
        [JsonProperty("nameSubstrings")] public string[] NameSubstrings { get; set; } = Array.Empty<string>();

    }
}