using System;
using System.Collections.Generic;
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
using SynthusMaximus.Data.DTOs.Armor;
using SynthusMaximus.Data.DTOs.Weapon;
using SynthusMaximus.Data.Enums;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;

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
        private readonly ArmorSettings _armorSettings;
        private readonly OverlayLoader _loader;
        private IDictionary<string, WeaponOverride> _weaponOverrides;
        private IList<WeaponType> _weaponTypes;
        private IDictionary<string,WeaponMaterial> _weaponMaterials;

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
            //_armorSettings = _loader.LoadObject<ArmorSettings>((RelativePath)@"armor\armorSettings.json");
            _armorModifiers = _loader.LoadList<ArmorModifier>((RelativePath)@"armor\armorModifiers.json");
            _armorMasqueradeBindings = _loader.LoadList<ArmorMasqueradeBinding>((RelativePath)@"armor\armorMasqueradeBindings.json");
            _armorMaterials = _loader.LoadDictionary<string, ArmorMaterial>((RelativePath)@"armor\armorMaterials.json");
            _armorReforgeExclusions = _loader.LoadValueConcatDictionary<ExclusionType, Regex>((RelativePath)@"exclusions\armor");

            _weaponOverrides =
                _loader.LoadDictionary<string, WeaponOverride>((RelativePath) @"weapons\weaponOverrides.json");
            _weaponTypes = _loader.LoadList<WeaponType>((RelativePath) @"weapons\weaponTypes.json");
            _weaponMaterials =
                _loader.LoadDictionary<string, WeaponMaterial>((RelativePath) @"weapons\weaponMaterials.json");
            _logger.LogInformation("Loaded data files in {MS}ms", sw.ElapsedMilliseconds);

            
        }



        public bool UseWarrior => _generalSettings.UseWarrior;
        public bool UseMage => _generalSettings.UseMage;
        public bool UseThief => _generalSettings.UseThief;


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
                return _armorSettings.MeltdownOutputFeet;
            if (a.HasAnyKeyword(ArmorHelmet, ClothingHead))
                return _armorSettings.MeltdownOutputHead;
            if (a.HasAnyKeyword(ArmorGauntlets, ClothingHands))
                return _armorSettings.MeltdownOutputHands;
            if (a.HasAnyKeyword(ArmorCuirass, ClothingBody))
                return _armorSettings.MeltdownOutputBody;
            if (a.HasKeyword(ArmorShield))
                return _armorSettings.MeltdownOutputShield;
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
                return _armorSettings.ArmorFactorFeet;
            
            if (a.HasKeyword(ArmorCuirass))
                return _armorSettings.ArmorFactorBody;
            
            if (a.HasKeyword(ArmorHelmet))
                return _armorSettings.ArmorFactorHead;
            
            if (a.HasKeyword(ArmorGauntlets))
                return _armorSettings.ArmorFactorHands;
            
            if (a.HasKeyword(ArmorShield))
                return _armorSettings.ArmorFactorShield;

            _logger.LogWarning("{EditorID}: no armor slot keyword", a.EditorID);

            return -1;
        }

        public bool IsArmorExcludedReforged(IArmorGetter a)
        {
            return _armorReforgeExclusions.Any(ex => CheckExclusionARMO(ex.Key, ex.Value, a));
        }

        private bool CheckExclusionARMO(ExclusionType ex, IEnumerable<Regex> patterns, IArmorGetter a)
        {
            if (ex == ExclusionType.Name || ex == ExclusionType.Full)
            {
                if (a.Name == null || !a.Name!.TryLookup(Language.English, out var name))
                    return false;
                return CheckExclusionName(patterns, name);
            }

            return CheckExclusionMajorRecord(ex, patterns, a);
        }

        private bool CheckExclusionMajorRecord(ExclusionType e, IEnumerable<Regex> patterns, IMajorRecordGetter m)
        {
            return e switch
            {
                ExclusionType.Name => throw new NotImplementedException("Should have been handled elsewhere"),
                ExclusionType.EDID => m.EditorID != null && patterns.Any(p => p.IsMatch(m.EditorID!)),
                ExclusionType.Full => throw new NotImplementedException("Should have been handled elsewhere"),
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

        public WeaponType? GetWeaponType(IWeaponGetter weaponGetter)
        {
            var name = weaponGetter.NameOrEmpty();
            return FindSingleBiggestSubstringMatch(_weaponTypes, name, wt => wt.NameSubStrings);
        }

        public WeaponMaterial? GetWeaponMaterial(IWeaponGetter weaponGetter)
        {
            var name = weaponGetter.NameOrEmpty();
            return FindSingleBiggestSubstringMatch(_weaponMaterials.Values, name, wt => wt.NameSubstrings);
        }
    }
}