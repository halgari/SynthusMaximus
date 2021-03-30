using System;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Alchemy
{
    public class AlchemyEffect
    {
        [JsonProperty("identifier")] public string Identifier { get; set; } = "";
        [JsonProperty("baseMagnitude")] public float BaseMagnitude { get; set; }
        [JsonProperty("baseDuration")] public float BaseDuration { get; set; }
        [JsonProperty("baseCost")] public float BaseCost { get; set; }
        [JsonProperty("allowIngredientVariation")] public bool AllowIngredientVariation { get; set; }
        [JsonProperty("allowPotionMultiplier")] public bool AllowPotionMultiplier { get; set; }
        [JsonProperty("nameSubstrings")] public string[] NameSubstrings { get; set; } = Array.Empty<string>();

    }
}