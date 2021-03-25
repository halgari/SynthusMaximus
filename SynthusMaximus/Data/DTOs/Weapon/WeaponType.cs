using System;
using Newtonsoft.Json;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.DTOs.Weapon
{
    public class WeaponType
    {
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("nameSubstrings")] public string[] NameSubStrings { get; set; } = Array.Empty<string>();
        [JsonProperty("baseWeaponType")] public DynamicEnum<BaseWeaponType>.DynamicEnumMember BaseWeaponType { get; set; }
        [JsonProperty("damageBase")] public ushort DamageBase { get; set; }
        [JsonProperty("reachBase")] public float ReachBase { get; set; }
        [JsonProperty("speedBase")] public float SpeedBase { get; set; }
        [JsonProperty("critDamageFactor")] public float CritDamageFactor { get; set; }
        [JsonProperty("meltdownOutput")] public ushort MeltdownOutput { get; set; }
        [JsonProperty("meltdownInput")] public ushort MeltdownInput { get; set; }
        [JsonProperty("bleedTier")] public DamageTier BleedTier { get; set; }
        [JsonProperty("debuffTier")] public DamageTier DebuffTier { get; set; }
        [JsonProperty("staggerTier")] public DamageTier StaggerTier { get; set; }
        [JsonProperty("weaponClass")] public DynamicEnum<WeaponClass>.DynamicEnumMember WeaponClass { get; set; }
    }
}