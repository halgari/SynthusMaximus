using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;
using Noggog;
using Wabbajack.Common;
using Armor = SynthusMaximus.Data.LowLevel.Armor;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using SynthusMaximus.Data.LowLevel;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;

namespace SynthusMaximus.Data
{
    public class DataStorage
    {
        private Armor _armor = new();
        private GeneralSettings _generalSettings = new();
        private ILogger<DataStorage> _logger;

        public DataStorage(ILogger<DataStorage> logger)
        {
            _logger = logger;
            _armor = AbsolutePath.EntryPoint.Combine(@"Resources\Armor.json").FromJson<Armor>();
            _generalSettings = AbsolutePath.EntryPoint.Combine(@"Resources\GeneralSettings.json")
                .FromJson<GeneralSettings>();
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

        private T? QuerySingleBindingInBindables<T>(string toMatch, List<ArmorBinding> bindings, List<T> bindables)
        where T : IBindable
        {
            var bestHit = GetBestBindingMatch(toMatch, bindings);
            if (bestHit == null)
                return default;
            
            return GetBindableFromIdentifier(bestHit, bindables);
        }

        private T? GetBindableFromIdentifier<T>(string identifier, List<T> list)
        where T : IBindable
        {
            return list.FirstOrDefault(b => b.Identifier == identifier);
        }

        private string? GetBestBindingMatch(string toMatch, IEnumerable<ArmorBinding> bindings)
        {
            var maxHitSize = 0;
            string? bestHit = null;
            
            foreach (var b in bindings.Where(b => toMatch.Contains(b.Substring)))
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
    }
}