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
using SynthusMaximus.Data.Enums;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;

namespace SynthusMaximus.Data
{
    public class DataStorage
    {
        private GeneralSettings _generalSettings = new();
        private ILogger<DataStorage> _logger;
        private IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
        private JsonSerializerSettings _serializerSettings;
        private IList<ArmorModifier> _armorModifiers;
        private IDictionary<string, Material> _materials;
        private IList<ArmorMasqueradeBinding> _armorMasqueradeBindings;
        private IDictionary<string, DTOs.Armor.ArmorMaterial> _armorMaterials;
        private IDictionary<ExclusionType, List<Regex>> _armorReforgeExclusions;
        private ArmorSettings _armorSettings;

        public DataStorage(ILogger<DataStorage> logger, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            _state = state;
            _logger = logger;


            //_armor = AbsolutePath.EntryPoint.Combine(@"Resources\Armors.json").FromJson<Armors>();
            //_weapons = AbsolutePath.EntryPoint.Combine(@"Resources\Weapons.json").FromJson<Weapons>();
            //_generalSettings = AbsolutePath.EntryPoint.Combine(@"Resources\GeneralSettings.json")
           //     .FromJson<GeneralSettings>();
            _serializerSettings = new JsonSerializerSettings();

            var sw = Stopwatch.StartNew();
            var converters = new List<Task<JsonConverter>>()
            {
                Task.Run(() => (JsonConverter) new PerkConverter(state)),
                Task.Run(() => (JsonConverter) new ItemConverter(state)),
                Task.Run(() => (JsonConverter) new KeywordConverter(state)),
            };

            _serializerSettings.Converters = converters.Select(c => c.Result)
                .Concat(new JsonConverter[]
                {
                    new BaseMaterialArmorConverter(),
                    new BaseMaterialArmorConverter(),
                    new MasqueradeFactionConverter(),
                    new ArmorClassConverter(),
                    new ExclusionTypeConverter()
                })
                .ToList();
            _logger.LogInformation("Loaded converters in {MS}ms", sw.ElapsedMilliseconds);
            
            sw.Restart();
            _materials = LoadDictionary<string, Material>("materials.json");
            _armorSettings = LoadObject<ArmorSettings>(@"armor\armorSettings.json");
            _armorModifiers = LoadList<ArmorModifier>(@"armor\armorModifiers.json");
            _armorMasqueradeBindings = LoadList<ArmorMasqueradeBinding>(@"armor\armorMasqueradeBindings.json");
            _armorMaterials = LoadDictionary<string, DTOs.Armor.ArmorMaterial>(@"armor\armorMaterials.json");
            _armorReforgeExclusions = LoadValueConcatDictionary<ExclusionType, Regex>(@"exclusions\armor");
            _logger.LogInformation("Loaded data files in {MS}ms", sw.ElapsedMilliseconds);

            
        }

        private T LoadObject<T>(string name)
        {
            T acc = default;
            foreach (var (modKey, _) in _state.LoadOrder)
            {
                var pathA = ((AbsolutePath) _state.DataFolderPath).Combine("config", modKey.Name, name);
                var pathB = AbsolutePath.EntryPoint.Combine("config", modKey.Name, name);
                T data;
                if (pathA.Exists)
                {
                    _logger.LogInformation("Loading {Path}", pathA);
                    data = JsonConvert.DeserializeObject<T>(pathA.ReadAllText(), _serializerSettings)!;
                }
                else if (pathB.Exists)
                {
                    _logger.LogInformation("Loading {Path}", pathB);
                    data = JsonConvert.DeserializeObject<T>(pathB.ReadAllText(), _serializerSettings)!;
                }
                else
                    continue;

                acc = data;

            }

            return acc;
        }

        private IDictionary<TK, List<TV>> LoadValueConcatDictionary<TK, TV>(string name)
            where TK : notnull
        {
            Dictionary<TK, List<TV>> acc = new();
            foreach (var (modKey, _) in _state.LoadOrder)
            {
                var pathA = ((AbsolutePath) _state.DataFolderPath).Combine("config", modKey.Name, name);
                var pathB = AbsolutePath.EntryPoint.Combine("config", modKey.Name, name);
                Dictionary<TK, List<TV>> data;
                if (pathA.Exists)
                {
                    _logger.LogInformation("Loading {Path}", pathA);
                    data = JsonConvert.DeserializeObject<Dictionary<TK, List<TV>>>(pathA.ReadAllText(), _serializerSettings)!;
                }
                else if (pathB.Exists)
                {
                    _logger.LogInformation("Loading {Path}", pathB);
                    data = JsonConvert.DeserializeObject<Dictionary<TK, List<TV>>>(pathB.ReadAllText(), _serializerSettings)!;
                }
                else
                    continue;

                foreach (var (key, value) in data)
                {
                    if (acc.TryGetValue(key, out var old))
                    {
                    }
                    else
                    {
                        old = new List<TV>();
                        acc[key] = old;
                    }
                    old.AddRange(value);
                }

            }
            return acc;
        }
        

        private IDictionary<TK, TV> LoadDictionary<TK, TV>(string name)
            where TK : notnull
        {
            Dictionary<TK, TV> acc = new();
            foreach (var (modKey, _) in _state.LoadOrder)
            {
                var pathA = ((AbsolutePath) _state.DataFolderPath).Combine("config", modKey.Name, name);
                var pathB = AbsolutePath.EntryPoint.Combine("config", modKey.Name, name);
                Dictionary<TK, TV> data;
                if (pathA.Exists)
                {
                    _logger.LogInformation("Loading {Path}", pathA);
                    data = JsonConvert.DeserializeObject<Dictionary<TK, TV>>(pathA.ReadAllText(), _serializerSettings)!;
                }
                else if (pathB.Exists)
                {
                    _logger.LogInformation("Loading {Path}", pathB);
                    data = JsonConvert.DeserializeObject<Dictionary<TK, TV>>(pathB.ReadAllText(), _serializerSettings)!;
                }
                else
                    continue;
                
                foreach (var (key, value) in data)
                    acc[key] = value;
                
            }

            return acc;
        }
        
        private IList<TV> LoadList<TV>(string name)
            where TV : notnull
        {
            List<TV> acc = new();
            foreach (var (modKey, _) in _state.LoadOrder)
            {
                var pathA = ((AbsolutePath) _state.DataFolderPath).Combine("config", modKey.Name, name);
                var pathB = AbsolutePath.EntryPoint.Combine("config", modKey.Name, name);
                List<TV> data;
                if (pathA.Exists)
                {
                    _logger.LogInformation("Loading {Path}", pathA);
                    data = JsonConvert.DeserializeObject<List<TV>>(pathA.ReadAllText(), _serializerSettings)!;
                }
                else if (pathB.Exists)
                {
                    _logger.LogInformation("Loading {Path}", pathB);
                    data = JsonConvert.DeserializeObject<List<TV>>(pathB.ReadAllText(), _serializerSettings)!;
                }
                else
                    continue;
                
                acc.AddRange(data);
            }

            return acc;
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

        private T2? QuerySingleBindingInBindables<T1, T2>(string toMatch, List<T1> bindings, List<T2> bindables)
        where T1 : IBinding
        where T2 : IBindable
        {
            var bestHit = GetBestBindingMatch(toMatch, bindings);
            if (bestHit == null)
                return default;
            
            return GetBindableFromIdentifier(bestHit, bindables);
        }

        private T? GetBindableFromIdentifier<T>(string identifier, IEnumerable<T> list)
        where T : IBindable
        {
            return list.FirstOrDefault(b => b.Identifier == identifier);
        }

        private string? GetBestBindingMatch<T>(string toMatch, IEnumerable<T> bindings)
        where T: IBinding
        {
            var maxHitSize = 0;
            string? bestHit = null;
            
            foreach (var b in bindings.Where(b => toMatch.Contains(b.SubString)))
            {
                string currHit = b.Identifier;
                var currHitSize = b.Identifier.Length;
                
                if (currHitSize <= maxHitSize) continue;
                
                maxHitSize = currHitSize;
                bestHit = currHit;
            }

            return bestHit;
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

        private IEnumerable<string> GetAllBindingMatches<T1>(string toMatch, IEnumerable<T1> bindings) where T1 : IBinding
        {
            return bindings.Where(b => toMatch.Contains(b.SubString)).Select(b => b.Identifier);
        }

        public IEnumerable<IFormLink<IKeywordGetter>> GetArmorMasqueradeKeywords(IArmorGetter a)
        {
            var name = a.NameOrThrow();
            return _armorMasqueradeBindings.Where(mb => mb.SubstringArmors.Any(s => name.Contains(s)))
                .Select(m => m.Faction.GetDefinition().Keyword)
                .Where(m => m != null)
                .Select(m => m!);
        }

        /*
        public WeaponOverride? GetWeaponOverride(IWeaponGetter w)
        {
            var name = w.NameOrThrow();
            return _weapons.WeaponOverrides.FirstOrDefault(o => o.FullName == name);
        }

        public object GetWeaponType(IWeaponGetter weaponGetter)
        {
            throw new NotImplementedException();
        }
        
        public WeaponMaterial? GetWeaponMaterial(IWeaponGetter w)
        {
            //return QuerySingleBindingInBindables(w.NameOrThrow(), _weapons.WeaponMaterialBindings.Binding,
            //    _weapons.WeaponMaterials.WeaponMaterial);
            return null;
        } */
    }
}