using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Weapon
{
    public class WeaponSettings
    {
        [JsonProperty("appendTypeToName")] public bool AppendTypeToName { get; set; }
        [JsonProperty("baseDamageLightWeaponry")] public float BaseDamageLightWeaponry { get; set; }
        [JsonProperty("baseDamageRangedWeaponry")] public float BaseDamageRangedWeaponry { get; set; }
        [JsonProperty("baseDamageHeavyWeaponry")] public float BaseDamageHeavyWeaponry { get; set; }
        [JsonProperty("damageFactorLightWeaponry")] public float DamageFactorLightWeaponry { get; set; }
        [JsonProperty("damageFactorRangedWeaponry")] public float DamageFactorRangedWeaponry { get; set; }
        [JsonProperty("damageFactorHeavyWeaponry")] public float DamageFactorHeavyWeaponry { get; set; }
    }
}