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
using System.Threading;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using SynthusMaximus.Data.LowLevel;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data.Converters;
using SynthusMaximus.Data.DTOs;
using SynthusMaximus.Data.Enums;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;

namespace SynthusMaximus.Data
{
    public class DataStorage
    {
        private Armors _armor = new();
        private Weapons _weapons = new();
        
        private GeneralSettings _generalSettings = new();
        private ILogger<DataStorage> _logger;
        private IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
        private JsonSerializerSettings _serializerSettings;
        private IList<ArmorModifier> _armorModifiers;
        private IDictionary<string, Material> _materials;
        private IList<ArmorMasqueradeBinding> _armorMasqueradeBindings;
        private IDictionary<string, DTOs.Armor.ArmorMaterial> _armorMaterials;

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
                    new ArmorClassConverter()
                })
                .ToList();
            _logger.LogInformation("Loaded converters in {MS}ms", sw.ElapsedMilliseconds);
            
            sw.Restart();
            _materials = LoadDictionary<string, Material>("materials.json");
            _armorModifiers = LoadList<ArmorModifier>(@"armor\armorModifiers.json");
            _armorMasqueradeBindings = LoadList<ArmorMasqueradeBinding>(@"armor\armorMasqueradeBindings.json");
            _armorMaterials = LoadDictionary<string, DTOs.Armor.ArmorMaterial>(@"armor\armorMaterials.json");
            _logger.LogInformation("Loaded data files in {MS}ms", sw.ElapsedMilliseconds);

            
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
                return (ushort)_armor.ArmorSettings.MeltdownOutputFeet;
            if (a.HasAnyKeyword(ArmorHelmet, ClothingHead))
                return (ushort)_armor.ArmorSettings.MeltdownOutputHead;
            if (a.HasAnyKeyword(ArmorGauntlets, ClothingHands))
                return (ushort)_armor.ArmorSettings.MeltdownOutputHands;
            if (a.HasAnyKeyword(ArmorCuirass, ClothingBody))
                return (ushort)_armor.ArmorSettings.MeltdownOutputBody;
            if (a.HasKeyword(ArmorShield))
                return (ushort)_armor.ArmorSettings.MeltdownOutputShield;
            return 0;
        }

        public ArmorMaterial? GetArmorMaterial(IArmorGetter a)
        {
            if (a.Name == null) return null;
            if (!a.Name.TryLookup(Language.English, out var toMatch))
                return null;
            return QuerySingleBindingInBindables(toMatch, 
                _armor.ArmorMaterialBindings.Binding,
                _armor.ArmorMaterials.ArmorMaterial);
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
                return _armor.ArmorSettings.ArmorFactorFeet;
            
            if (a.HasKeyword(ArmorCuirass))
                return _armor.ArmorSettings.ArmorFactorBody;
            
            if (a.HasKeyword(ArmorHelmet))
                return _armor.ArmorSettings.ArmorFactorHead;
            
            if (a.HasKeyword(ArmorGauntlets))
                return _armor.ArmorSettings.ArmorFactorHands;
            
            if (a.HasKeyword(ArmorShield))
                return _armor.ArmorSettings.ArmorFactorShield;

            _logger.LogWarning("{EditorID}: no armor slot keyword", a.EditorID);

            return -1;
        }

        public bool IsArmorExcludedReforged(IArmorGetter a)
        {
            return _armor.ReforgeExclusions.Exclusion.Any(ex => CheckExclusionARMO(ex, a));
        }

        private bool CheckExclusionARMO(Exclusion ex, IArmorGetter a)
        {
            if (ex.Target == Exclusion.TargetType.Name)
            {
                if (!a.Name.TryLookup(Language.English, out var name))
                    return false;
                return CheckExclusionName(ex, name);
            }

            return CheckExclusionMajorRecord(ex, a);
        }

        private bool CheckExclusionMajorRecord(Exclusion e, ISkyrimMajorRecordGetter m)
        {
            string toCheck = e.Target switch
            {
                Exclusion.TargetType.EDID => m.EditorID ?? "",
                Exclusion.TargetType.FormID => m.FormKey.ToString(),
                _ => throw new ArgumentException($"Exclusion has an invalid target: {e.Target}")
            };

            return e.Type switch
            {
                Exclusion.ExclusionType.Contains => toCheck.Contains(e.Text),
                Exclusion.ExclusionType.Equals => string.Equals(toCheck, e.Text),
                Exclusion.ExclusionType.EqualsIgnoreCase => string.Equals(toCheck, e.Text,
                    StringComparison.InvariantCultureIgnoreCase),
                Exclusion.ExclusionType.StartsWith => toCheck.StartsWith(e.Text),
                _ => throw new ArgumentOutOfRangeException($"Exclusion has invalid type {e.Type}")
            };

        }

        private bool CheckExclusionName(Exclusion e, string name)
        {
            return e.Type switch
            {
                Exclusion.ExclusionType.Contains => name.Contains(e.Text),
                Exclusion.ExclusionType.Equals => string.Equals(name, e.Text),
                Exclusion.ExclusionType.EqualsIgnoreCase => string.Equals(name, e.Text,
                    StringComparison.InvariantCultureIgnoreCase),
                Exclusion.ExclusionType.StartsWith => name.StartsWith(e.Text),
                _ => throw new ArgumentOutOfRangeException($"Exclusion has invalid type {e.Type}")
            };
        }

        public string GetOutputString(string sReforged)
        {
            return sReforged;
        }

        public IEnumerable<ArmorModifier> GetArmorModifiers(IArmorGetter a)
        {
            if (!a.Name!.TryLookup(Language.English, out var name))
                throw new InvalidDataException("Couldn't load name");

            return QueryAllBindingsInBindables(name, _armor.ArmorModifierBindings.Binding,
                _armor.ArmorModifiers.ArmorModifier);
        }

        private IEnumerable<T2> QueryAllBindingsInBindables<T1, T2>(string toMatch, IEnumerable<T1> bindings, IEnumerable<T2> bindables)
        where T1 : IBinding
        where T2 : IBindable
        {
            var hits = GetAllBindingMatches(toMatch, bindings);

            foreach (var hit in hits)
            {
                var bindable = GetBindableFromIdentifier(hit, bindables);
                if (bindable != null)
                    yield return bindable;
            }
        }

        private IEnumerable<string> GetAllBindingMatches<T1>(string toMatch, IEnumerable<T1> bindings) where T1 : IBinding
        {
            return bindings.Where(b => toMatch.Contains(b.SubString)).Select(b => b.Identifier);
        }

        public IEnumerable<IFormLink<IKeywordGetter>> GetArmorMasqueradeKeywords(IArmorGetter a)
        {
            if (!a.Name!.TryLookup(Language.English, out var name))
                return Array.Empty<IFormLink<IKeywordGetter>>();

            return _armor.ArmorMasqueradeBindings.ArmorMasqueradeBinding
                .Where(m => name.Contains(m.SubstringArmor))
                .Select(m => m.MasqueradeFaction.GetDefinition().Keyword)
                .Where(m => m != null)
                .Select(m => m!);
        }

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
        }
    }
}