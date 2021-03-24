using System.Collections.Generic;
using Newtonsoft.Json;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.LowLevel
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class WeaponSettings
    {
        [JsonProperty("appendTypeToName")] public bool AppendTypeToName { get; set; }

        [JsonProperty("baseDamageLightWeaponry")]
        public int BaseDamageLightWeaponry { get; set; }

        [JsonProperty("baseDamageRangedWeaponry")]
        public int BaseDamageRangedWeaponry { get; set; }

        [JsonProperty("baseDamageHeavyWeaponry")]
        public int BaseDamageHeavyWeaponry { get; set; }

        [JsonProperty("damageFactorLightWeaponry")]
        public int DamageFactorLightWeaponry { get; set; }

        [JsonProperty("damageFactorRangedWeaponry")]
        public int DamageFactorRangedWeaponry { get; set; }

        [JsonProperty("damageFactorHeavyWeaponry")]
        public int DamageFactorHeavyWeaponry { get; set; }
    }

    public class Binding
    {
        [JsonProperty("substring")] public string Substring { get; set; }

        [JsonProperty("identifier")] public object Identifier { get; set; }
    }

    public class WeaponModifierBindings
    {
        [JsonProperty("binding")] public List<Binding> Binding { get; set; }
    }

    public class WeaponModifier
    {
        [JsonProperty("identifier")] public object Identifier { get; set; }

        [JsonProperty("factorDamage")] public double FactorDamage { get; set; }

        [JsonProperty("factorCritDamage")] public double FactorCritDamage { get; set; }

        [JsonProperty("factorWeight")] public double FactorWeight { get; set; }

        [JsonProperty("factorReach")] public double FactorReach { get; set; }

        [JsonProperty("factorAttackSpeed")] public double FactorAttackSpeed { get; set; }

        [JsonProperty("factorValue")] public double FactorValue { get; set; }
    }

    public class WeaponModifiers
    {
        [JsonProperty("weapon_modifier")] public List<WeaponModifier> WeaponModifier { get; set; }
    }

    public class Bindings
    {
        [JsonProperty("identifier")] public string Identifier { get; set; }

        [JsonProperty("substring")] public string Substring { get; set; }
    }

    public class WeaponMaterialBindings
    {
        [JsonProperty("#text")] public string Text { get; set; }

        [JsonProperty("binding")] public List<Binding> Binding { get; set; }

        [JsonProperty("bindings")] public Bindings Bindings { get; set; }
    }

    public class WeaponMaterial
    {
        [JsonProperty("damageModifier")] public double DamageModifier { get; set; }

        [JsonProperty("identifier")] public string Identifier { get; set; }

        [JsonProperty("materialMeltdown")]
        public BaseMaterialWeapon MaterialMeltdown { get; set; } = BaseMaterialWeapon.None;

        [JsonProperty("materialTemper")]
        public BaseMaterialWeapon MaterialTemper { get; set; } = BaseMaterialWeapon.None;

        [JsonProperty("reachModifier")] public double ReachModifier { get; set; }

        [JsonProperty("speedModifier")] public double SpeedModifier { get; set; }
    }

    public class WeaponMaterials
    {
        [JsonProperty("weapon_material")] public List<WeaponMaterial> WeaponMaterial { get; set; }
    }

    public class Bindin
    {
        [JsonProperty("substring")] public string Substring { get; set; }

        [JsonProperty("identifier")] public string Identifier { get; set; }
    }

    public class WeaponTypeBindings
    {
        [JsonProperty("binding")] public List<Binding> Binding { get; set; }
    }

    public class WeaponType
    {
        [JsonProperty("identifier")] public string Identifier { get; set; }

        [JsonProperty("baseWeaponType")] public string BaseWeaponType { get; set; }

        [JsonProperty("damageBase")] public double DamageBase { get; set; }

        [JsonProperty("reachBase")] public double ReachBase { get; set; }

        [JsonProperty("speedBase")] public double SpeedBase { get; set; }

        [JsonProperty("critDamageFactor")] public double CritDamageFactor { get; set; }

        [JsonProperty("meltdownOutput")] public int MeltdownOutput { get; set; }

        [JsonProperty("meltdownInput")] public int MeltdownInput { get; set; }

        [JsonProperty("bleedTier")] public string BleedTier { get; set; }

        [JsonProperty("debuffTier")] public string DebuffTier { get; set; }

        [JsonProperty("staggerTier")] public string StaggerTier { get; set; }

        [JsonProperty("weaponClass")] public string WeaponClass { get; set; }
    }

    public class WeaponTypes
    {
        [JsonProperty("weapon_type")] public List<WeaponType> WeaponType { get; set; }
    }

    public class WeaponOverride
    {
        [JsonProperty("fullName")] public string FullName { get; set; }

        [JsonProperty("baseWeaponType")] public string BaseWeaponType { get; set; }

        [JsonProperty("damage")] public ushort Damage { get; set; }

        [JsonProperty("critDamage")] public ushort CritDamage { get; set; }

        [JsonProperty("materialMeltdown")] public BaseMaterialWeapon MaterialMeltdown { get; set; }

        [JsonProperty("materialTempering")] public BaseMaterialWeapon MaterialTempering { get; set; }

        [JsonProperty("meltdownOutput")] public ushort MeltdownOutput { get; set; }

        [JsonProperty("meltdownInput")] public ushort MeltdownInput { get; set; }

        [JsonProperty("reach")] public float Reach { get; set; }

        [JsonProperty("speed")] public float Speed { get; set; }

        [JsonProperty("stringToAppend")] public string StringToAppend { get; set; }
    }

    public class Weapons
    {
        [JsonProperty("weapon_settings")] public WeaponSettings WeaponSettings { get; set; }

        [JsonProperty("weapon_modifier_bindings")]
        public WeaponModifierBindings WeaponModifierBindings { get; set; }

        [JsonProperty("weapon_modifiers")] public WeaponModifiers WeaponModifiers { get; set; }

        [JsonProperty("weapon_material_bindings")]
        public WeaponMaterialBindings WeaponMaterialBindings { get; set; }

        [JsonProperty("weapon_materials")] public WeaponMaterials WeaponMaterials { get; set; }

        [JsonProperty("weapon_type_bindings")] public WeaponTypeBindings WeaponTypeBindings { get; set; }

        [JsonProperty("weapon_types")] public WeaponTypes WeaponTypes { get; set; }

        [JsonProperty("weapon_overrides")] public List<WeaponOverride> WeaponOverrides { get; set; }

        [JsonProperty("reforge_exclusions")] public ReforgeExclusions ReforgeExclusions { get; set; }
    }
}