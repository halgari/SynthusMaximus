using System;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Alchemy
{
    public class IngredientVariation
    {
        [JsonProperty("identifier")] public string Identifier { get; set; } = "";
        [JsonProperty("multiplierMagnitude")] public float MultiplierMagnitude { get; set; }
        [JsonProperty("multiplierDuration")] public float MultiplierDuration { get; set; }
        [JsonProperty("nameSubstrings")] public string[] NameSubstrings { get; set; } = Array.Empty<string>();
    }
}