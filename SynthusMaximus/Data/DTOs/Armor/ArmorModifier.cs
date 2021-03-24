using System;
using Newtonsoft.Json;
using SynthusMaximus.Data.LowLevel;

namespace SynthusMaximus.Data.DTOs.Armor
{
    public class ArmorModifier
    {
        [JsonProperty("name")] public string Identifier { get; set; } = "";

        [JsonProperty("nameSubStrings")] public string[] SubStrings { get; set; } = Array.Empty<string>();

        [JsonProperty("factorArmor")] public float FactorArmor { get; set; }

        [JsonProperty("factorValue")] public float FactorValue { get; set; }

        [JsonProperty("factorWeight")] public float FactorWeight { get; set; }
    }

}