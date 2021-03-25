using System;
using Newtonsoft.Json;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.DTOs.Weapon
{
    public class WeaponMaterial
    {
        [JsonProperty("type")] public DynamicEnum<Material>.DynamicEnumMember Type { get; set; }
        [JsonProperty("damageModifier")] public float DamageModifier { get; set; }
        [JsonProperty("reachModifier")] public float ReachModifier { get; set; }
        [JsonProperty("speedModifier")] public float SpeedModifier { get; set; }
        [JsonProperty("nameSubstrings")] public string[] NameSubstrings { get; set; } = Array.Empty<string>();

    }
}