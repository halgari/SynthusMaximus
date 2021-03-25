using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.DTOs.Weapon
{
    public class WeaponOverride
    {
        [JsonProperty("baseWeaponType")] public DynamicEnum<BaseWeaponType>.DynamicEnumMember Class { get; set; }
        [JsonProperty("damage")] public ushort Damage { get; set; }
        [JsonProperty("critDamage")] public ushort CritDamage { get; set; }
        [JsonProperty("materialMeltdown")] public DynamicEnum<Material>.DynamicEnumMember MaterialMeltdown { get; set; }
        [JsonProperty("materialTempering")] public DynamicEnum<Material>.DynamicEnumMember MaterialTempering { get; set; }
        [JsonProperty("meltdownOutput")] public ushort MeltdownOutput { get; set; }
        [JsonProperty("meltdownInput")] public ushort MeltdownInput { get; set; }
        [JsonProperty("reach")] public float Reach { get; set; }
        [JsonProperty("speed")] public float Speed { get; set; }
        [JsonProperty("stringToAppend")] public string StringToAppend { get; set; } = "";
    }
}