using System;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Weapon
{
    public class WeaponModifier
    {
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("nameSubstrings")] public string[] NameSubstrings { get; set; } = Array.Empty<string>();
        [JsonProperty("factorDamage")] public float FactorDamage { get; set; }
        [JsonProperty("factorCritDamage")] public float FactorCritDamage { get; set; }
        [JsonProperty("factorWeight")] public float FactorWeight { get; set; }
        [JsonProperty("factorReach")] public float FactorReach { get; set; }
        [JsonProperty("factorAttackSpeed")] public float FactorAttackSpeed { get; set; }
        [JsonProperty("factorValue")] public float FactorValue { get; set; }
    }
}