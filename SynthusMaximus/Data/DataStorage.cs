using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;
using Noggog;
using Wabbajack.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using SynthusMaximus.Data.LowLevel;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data.Converters;
using SynthusMaximus.Data.DTOs;
using SynthusMaximus.Data.DTOs.Alchemy;
using SynthusMaximus.Data.DTOs.Ammunition;
using SynthusMaximus.Data.DTOs.Armor;
using SynthusMaximus.Data.DTOs.Enchantment;
using SynthusMaximus.Data.DTOs.Weapon;
using SynthusMaximus.Data.Enums;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;

namespace SynthusMaximus.Data
{
    public class DataStorage
    {
        private readonly GeneralSettings _generalSettings = new();
        private readonly ILogger<DataStorage> _logger;
        private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly IList<ArmorModifier> _armorModifiers;
        private readonly IList<ArmorMasqueradeBinding> _armorMasqueradeBindings;
        private readonly IDictionary<string, ArmorMaterial> _armorMaterials;
        private readonly IDictionary<ExclusionType, List<Regex>> _armorReforgeExclusions;
        public ArmorSettings ArmorSettings { get; }
        private readonly OverlayLoader _loader;
        private readonly IDictionary<string, WeaponOverride> _weaponOverrides;
        private readonly IList<WeaponType> _weaponTypes;
        private readonly IDictionary<string,WeaponMaterial> _weaponMaterials;
        private readonly IList<WeaponModifier> _weaponModifiers;
        private readonly WeaponSettings _weaponSettings;
        private readonly IDictionary<ExclusionType, List<Regex>> _weaponReforgeExclusions;
        private readonly IDictionary<ExclusionType, List<Regex>> _distributionExclusionsWeaponRegular;
        private readonly IDictionary<ExclusionType, List<Regex>> _distributionExclusionsWeaponListRegular;
        private readonly IDictionary<ExclusionType, List<Regex>> _potionExclusions;
        private readonly IList<AlchemyEffect> _alchemyEffect;
        private readonly IList<PotionMultiplier> _potionMultipliers;
        private readonly IList<IngredientVariation> _ingredientVariations;
        private readonly IDictionary<ExclusionType, List<Regex>> _ingredientExclusions;
        private IDictionary<ExclusionType, List<Regex>> _ammunitionExclusionsMultiplication;
        private readonly IList<AmmunitionType> _ammunitionTypes;
        private IList<AmmunitionMaterial> _ammunitionMaterials;
        private IList<AmmunitionModifier> _ammunitionModifer;
        public ExclusionList<INpcGetter> NPCExclusions { get; }


        public DataStorage(ILogger<DataStorage> logger, 
            IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
            IEnumerable<IInjectedConverter> converters,
            OverlayLoader loader)
        {
            _state = state;
            _logger = logger;
            _loader = loader;
            
            _loader.Converters = converters.Cast<JsonConverter>().ToArray();

            var sw = Stopwatch.StartNew();
            ArmorSettings = _loader.LoadObject<ArmorSettings>((RelativePath)@"armor\armorSettings.json");
            _armorModifiers = _loader.LoadList<ArmorModifier>((RelativePath)@"armor\armorModifiers.json");
            _armorMasqueradeBindings = _loader.LoadList<ArmorMasqueradeBinding>((RelativePath)@"armor\armorMasqueradeBindings.json");
            _armorMaterials = _loader.LoadDictionary<string, ArmorMaterial>((RelativePath)@"armor\armorMaterials.json");
            _armorReforgeExclusions = _loader.LoadValueConcatDictionary<ExclusionType, Regex>((RelativePath)@"exclusions\armorReforge.json");
            EnchantmentArmorExclusions =
                _loader.LoadExclusionList<IArmorGetter>((RelativePath) @"exclusions\enchantmentArmorExclusions.json");

            _weaponOverrides =
                _loader.LoadDictionary<string, WeaponOverride>((RelativePath) @"weapons\weaponOverrides.json");
            _weaponTypes = _loader.LoadList<WeaponType>((RelativePath) @"weapons\weaponTypes.json");
            _weaponMaterials =
                _loader.LoadDictionary<string, WeaponMaterial>((RelativePath) @"weapons\weaponMaterials.json");
            _weaponModifiers =
                _loader.LoadList<WeaponModifier>((RelativePath) @"weapons\weaponModifiers.json");
            _weaponSettings =
                _loader.LoadObject<WeaponSettings>((RelativePath) @"weapons\weaponSettings.json");
            _weaponReforgeExclusions =
                _loader.LoadValueConcatDictionary<ExclusionType, Regex>(
                    (RelativePath) @"exclusions\weaponReforge.json");
            _distributionExclusionsWeaponRegular =
                _loader.LoadValueConcatDictionary<ExclusionType, Regex>(
                    (RelativePath) @"exclusions\distributionExclusionsWeaponRegular.json");
            _distributionExclusionsWeaponListRegular =
                _loader.LoadValueConcatDictionary<ExclusionType, Regex>(
                (RelativePath) @"exclusions\distributionExclusionsWeaponListRegular.json");

            // Alchemy
            _potionExclusions =
                _loader.LoadValueConcatDictionary<ExclusionType, Regex>((RelativePath) @"exclusions\potionExclusions");
            _ingredientExclusions =
                _loader.LoadValueConcatDictionary<ExclusionType, Regex>((RelativePath) @"exclusions\ingredientExclusions");
            _alchemyEffect =
                _loader.LoadList<AlchemyEffect>((RelativePath) @"alchemy\alchemyEffects.json");
            _potionMultipliers =
                _loader.LoadList<PotionMultiplier>((RelativePath) @"alchemy\potionMultiplier.json");
            _ingredientVariations =
                _loader.LoadList<IngredientVariation>((RelativePath) @"alchemy\ingredientVariations.json");
            
            // Ammunition
            _ammunitionExclusionsMultiplication =
            _loader.LoadValueConcatDictionary<ExclusionType, Regex>(
                (RelativePath) @"ammunition\ammunitionExclusionsMultiplication.json");

            _ammunitionTypes = _loader.LoadList<AmmunitionType>((RelativePath) @"ammunition\ammunitionTypes.json");
            _ammunitionMaterials = _loader.LoadList<AmmunitionMaterial>((RelativePath) @"ammunition\ammunitionMaterials.json");
            _ammunitionModifer = _loader.LoadList<AmmunitionModifier>((RelativePath) @"ammunition\ammunitionModifiers.json");

            ScrollCraftingExclusions =
                _loader.LoadExclusionList<ITranslatedNamedGetter>((RelativePath) @"exclusions\scrollCrafting.json");
            StaffCraftingExclusions =
                _loader.LoadExclusionList<ITranslatedNamedGetter>((RelativePath) @"exclusions\staffCrafting.json");
            StaffCraftingDisableCraftingExclusions =
                _loader.LoadExclusionList<ITranslatedNamedGetter>(
                    (RelativePath) @"exclusions\staffCraftingDisableCraftingExclusions.json");
                    
            SpellDistributionExclusions =
                _loader.LoadExclusionList<ITranslatedNamedGetter>((RelativePath) @"exclusions\distributionExclusionsSpell.json");

            NPCExclusions = _loader.LoadExclusionList<INpcGetter>((RelativePath) @"exclusions\npc.json");
            RaceExclusions = _loader.LoadExclusionList<IRaceGetter>((RelativePath) @"exclusions\race.json");
            DistributionExclusionsArmor =
                _loader.LoadMajorRecordExclusionList<ILeveledItemGetter>(
                    (RelativePath) @"exclusions\distributionExclusionsArmor.json");
            ListEnchantmentBindings =
                _loader.LoadList<ListEnchantmentBinding>((RelativePath) @"enchanting\listEnchantmentBindings.json");
            DirectEnchantmentBindings =
                _loader.LoadList<DirectEnchantmentBinding>((RelativePath) @"enchanting\directEnchantmentBindings.json")
                    .ToLookup(b => b.Base);
            EnchantmentNames =
                _loader.LoadList<EnchantmentNameBinding>(
                    (RelativePath) @"enchanting\nameBindings.json")
                    .GroupBy(e => e.Enchantment)
                    .ToDictionary(e => e.Key, e => e.Last());

            _logger.LogInformation("Loaded data files in {MS}ms", sw.ElapsedMilliseconds);

            
        }

        public Dictionary<IFormLink<IObjectEffectGetter>, EnchantmentNameBinding> EnchantmentNames { get; set; }

        public ILookup<IFormLink<IObjectEffectGetter>, DirectEnchantmentBinding> DirectEnchantmentBindings { get; }

        public MajorRecordExclusionList<ILeveledItemGetter> DistributionExclusionsArmor { get; }





        public ExclusionList<IArmorGetter> EnchantmentArmorExclusions { get; }

        public IList<ListEnchantmentBinding> ListEnchantmentBindings { get; set; }

        public ExclusionList<IRaceGetter> RaceExclusions { get; }


        public ExclusionList<ITranslatedNamedGetter> SpellDistributionExclusions { get; }

        public ExclusionList<ITranslatedNamedGetter> StaffCraftingDisableCraftingExclusions { get; }
        public ExclusionList<ITranslatedNamedGetter> StaffCraftingExclusions { get; }
        public ExclusionList<ITranslatedNamedGetter> ScrollCraftingExclusions { get; }


        public bool UseWarrior => _generalSettings.UseWarrior;
        public bool UseMage => _generalSettings.UseMage;
        public bool UseThief => _generalSettings.UseThief;

        public bool ShouldRemoveUnspecificSpells => _generalSettings.RemoveUnspecificStartingSpells;
        public bool ShouldAppendWeaponType => _weaponSettings.AppendTypeToName;


        public static bool IsJewelry(IArmorGetter a)
        {
            return HasKeyword(a.Keywords, Statics.JewelryKeywords);
        }

        public static bool IsClothing(IArmorGetter a)
        {
            return HasKeyword(a.Keywords, Statics.ClothingKeywords);
        }

        public static bool HasKeyword(IReadOnlyList<IFormLinkGetter<IKeywordGetter>>? coll, IEnumerable<IFormLink<IKeywordGetter>> keywords)
        {
            return coll?.Any(keywords.Contains) ?? false;
        }

        public ushort GetArmorMeltdownOutput(IArmorGetter a)
        {
            if (a.HasAnyKeyword(ArmorBoots, ClothingFeet))
                return ArmorSettings.MeltdownOutputFeet;
            if (a.HasAnyKeyword(ArmorHelmet, ClothingHead))
                return ArmorSettings.MeltdownOutputHead;
            if (a.HasAnyKeyword(ArmorGauntlets, ClothingHands))
                return ArmorSettings.MeltdownOutputHands;
            if (a.HasAnyKeyword(ArmorCuirass, ClothingBody))
                return ArmorSettings.MeltdownOutputBody;
            if (a.HasKeyword(ArmorShield))
                return ArmorSettings.MeltdownOutputShield;
            return 0;
        }

        public ArmorMaterial? GetArmorMaterial(IArmorGetter a)
        {
            return FindSingleBiggestSubstringMatch(_armorMaterials.Values, a.NameOrEmpty(), m => m.SubStrings);
        }

        private static T? FindSingleBiggestSubstringMatch<T>(IEnumerable<T> coll, string toMatch, Func<T, IEnumerable<string>> substringSelector)
        {
            T? bestMatch = default;
            var maxHitSize = 0;

            foreach (var item in coll)
            {
                foreach (var substring in substringSelector(item))
                {
                    if (!toMatch.Contains(substring)) continue;
                    if (substring.Length <= maxHitSize) continue;
                    
                    maxHitSize = substring.Length;
                    bestMatch = item;
                }
            }
            return bestMatch;

        }

        public float GetArmorSlotMultiplier(IArmorGetter a)
        {
            if (a.HasKeyword(ArmorBoots))
                return ArmorSettings.ArmorFactorFeet;
            
            if (a.HasKeyword(ArmorCuirass))
                return ArmorSettings.ArmorFactorBody;
            
            if (a.HasKeyword(ArmorHelmet))
                return ArmorSettings.ArmorFactorHead;
            
            if (a.HasKeyword(ArmorGauntlets))
                return ArmorSettings.ArmorFactorHands;
            
            if (a.HasKeyword(ArmorShield))
                return ArmorSettings.ArmorFactorShield;

            _logger.LogWarning("{EditorID}: no armor slot keyword", a.EditorID);

            return -1;
        }

        public bool IsArmorExcludedReforged(IArmorGetter a)
        {
            return _armorReforgeExclusions.Any(ex => CheckExclusion(ex.Key, ex.Value, a));
        }

        private bool CheckExclusion(ExclusionType ex, IReadOnlyCollection<Regex> patterns, ITranslatedNamedGetter a)
        {
            if (ex == ExclusionType.Name || ex == ExclusionType.Full)
            {
                if (a.Name == null || !a.Name!.TryLookup(Language.English, out var name))
                    return false;
                return CheckExclusionName(patterns, name);
            }

            return CheckExclusionMajorRecord(ex, patterns, (IMajorRecordGetter)a);
        }

        private bool CheckExclusionMajorRecord(ExclusionType e, IReadOnlyCollection<Regex> patterns, IMajorRecordGetter m)
        {
            var fis = m.FormKey.ID.ToString("X8");
            if (!patterns.Any())
                return false;
            
            return e switch
            {
                ExclusionType.Name => throw new NotImplementedException("Should have been handled elsewhere"),
                ExclusionType.EDID => m.EditorID != null && patterns.Any(p => p.IsMatch(m.EditorID!)),
                ExclusionType.Full => throw new NotImplementedException("Should have been handled elsewhere"),
                ExclusionType.FormID => patterns.Any(p => p.IsMatch(fis)),
                _ => throw new ArgumentOutOfRangeException(nameof(e), e, null)
            };
        }

        private bool CheckExclusionName(IEnumerable<Regex> patterns, string name)
        {
            return patterns.Any(p => p.IsMatch(name));
        }

        public string GetOutputString(string sReforged)
        {
            return sReforged;
        }

        public IEnumerable<ArmorModifier> GetArmorModifiers(IArmorGetter a)
        {
            return AllMatchingBindings(_armorModifiers, a.NameOrThrow(), m => m.SubStrings);
        }

        private IEnumerable<T> AllMatchingBindings<T>(IEnumerable<T> bindings, string toMatch, Func<T, IEnumerable<string>> selector)
        {
            return bindings.Where(b => selector(b).Any(toMatch.Contains));
        }

        public IEnumerable<IFormLink<IKeywordGetter>> GetArmorMasqueradeKeywords(IArmorGetter a)
        {
            var name = a.NameOrThrow();
            return _armorMasqueradeBindings.Where(mb => mb.SubstringArmors.Any(s => name.Contains(s)))
                .Select(m => m.Faction.GetDefinition().Keyword)
                .Where(m => m != null)
                .Select(m => m!);
        }

        
        public WeaponOverride? GetWeaponOverride(IWeaponGetter w)
        {
            return _weaponOverrides.TryGetValue(w.NameOrThrow(), out var o) ? o : default;
        }

        private static Dictionary<string, WeaponType?> _weaponTypeCache = new();
        public WeaponType? GetWeaponType(IWeaponGetter weaponGetter)
        {
            var name = weaponGetter.NameOrEmpty();
            if (_weaponTypeCache.TryGetValue(name, out var type))
                return type;
            
            var found =  FindSingleBiggestSubstringMatch(_weaponTypes, name, wt => wt.NameSubStrings);
            _weaponTypeCache.Add(name, found);
            return found;
        }

        private static Dictionary<string, WeaponMaterial?> _weaponMaterialCache = new();


        public WeaponMaterial? GetWeaponMaterial(IWeaponGetter weaponGetter)
        {
            var name = weaponGetter.NameOrEmpty();
            if (_weaponMaterialCache.TryGetValue(name, out var type))
                return type;
            var found = FindSingleBiggestSubstringMatch(_weaponMaterials.Values, name, wt => wt.NameSubstrings);
            _weaponMaterialCache.Add(name, found);
            return found;
        }

        public float? GetWeaponSkillDamageBase(DynamicEnum<BaseWeaponType>.DynamicEnumMember wtBaseWeaponType)
        {
            var school = wtBaseWeaponType.Data.School;
            if (Equals(school, xMAWeapSchoolHeavyWeaponry))
                return _weaponSettings.BaseDamageHeavyWeaponry;
            if (Equals(school, xMAWeapSchoolRangedWeaponry))
                return _weaponSettings.BaseDamageRangedWeaponry;
            if (Equals(school, xMAWeapSchoolLightWeaponry))
                return _weaponSettings.BaseDamageLightWeaponry;

            return null;
        }

        public float? GetWeaponSkillDamageMultipler(DynamicEnum<BaseWeaponType>.DynamicEnumMember wtBaseWeaponType)
        {
            var school = wtBaseWeaponType.Data.School;
            if (Equals(school, xMAWeapSchoolHeavyWeaponry))
                return _weaponSettings.DamageFactorHeavyWeaponry;
            if (Equals(school, xMAWeapSchoolRangedWeaponry))
                return _weaponSettings.DamageFactorRangedWeaponry;
            if (Equals(school, xMAWeapSchoolLightWeaponry))
                return _weaponSettings.DamageFactorLightWeaponry;

            return null;
        }

        public bool IsWeaponExcludedReforged(IWeaponGetter w)
        {
            return _weaponReforgeExclusions.Any(ex => CheckExclusion(ex.Key, ex.Value, w));
        }

        public IEnumerable<WeaponModifier> GetAllModifiers(Weapon w)
        {
            var name = w.NameOrThrow();

            return AllMatchingBindings(_weaponModifiers, name, m => m.NameSubstrings);
        }

        public bool IsWeaponExcludedDistribution(IWeaponGetter w)
        {
            return _distributionExclusionsWeaponRegular.Any(ex => CheckExclusion(ex.Key, ex.Value, w));
        }

        public bool IsListExcludedWeaponRegular(ILeveledItemGetter li)
        {
            return _distributionExclusionsWeaponListRegular.Any(ex => CheckExclusionMajorRecord(ex.Key, ex.Value, li));
        }

        public bool IsAlchemyExcluded(ITranslatedNamedGetter a)
        {
            return _potionExclusions.Any(ex => CheckExclusion(ex.Key, ex.Value, a));
        }

        public AlchemyEffect? GetAlchemyEffect(IMagicEffectGetter m)
        {
            return FindSingleBiggestSubstringMatch(_alchemyEffect, m.NameOrThrow(), e => e.NameSubstrings);
        }

        public PotionMultiplier? GetPotionMultipiler(IIngestibleGetter i)
        {
            return FindSingleBiggestSubstringMatch(_potionMultipliers, i.NameOrThrow(), e => e.NameSubstrings);
        }

        public bool IsIngredientExcluded(IIngredientGetter i)
        {
            return _ingredientExclusions.Any(ex => CheckExclusion(ex.Key, ex.Value, i));
        }

        public IngredientVariation? GetIngredientVariation(IIngredientGetter ig)
        {
            return FindSingleBiggestSubstringMatch(_ingredientVariations, ig.NameOrThrow(), i => i.NameSubstrings);
        }

        public AmmunitionType? GetAmmunitionType(IAmmunitionGetter ammo)
        {
            return FindSingleBiggestSubstringMatch(_ammunitionTypes, ammo.NameOrThrow(), a => a.NameSubstrings);
        }

        public AmmunitionMaterial? GetAmmunitionMaterial(IAmmunitionGetter ammo)
        {
            return FindSingleBiggestSubstringMatch(_ammunitionMaterials, ammo.NameOrThrow(), a => a.NameSubstrings);
        }

        public IEnumerable<AmmunitionModifier> GetAmmunitionModifiers(IAmmunitionGetter a)
        {
            return AllMatchingBindings(_ammunitionModifer, a.NameOrThrow(), a => a.NameSubstrings);
        }

        public string GetLocalizedEnchantmentNameArmor(IArmorGetter template, IFormLink<IObjectEffectGetter> formLink)
        {
            if (!EnchantmentNames.TryGetValue(formLink, out var fstr)) return template.NameOrEmpty();
            return string.Format(fstr.NameTemplate, template.NameOrEmpty());
        }
    }
}