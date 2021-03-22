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
            _logger.LogInformation("{ToMatch} best Hit {BestHit}", toMatch, bestHit);
            if (bestHit == null)
                return default;

            _logger.LogInformation("{BestHit} - {Options}", bestHit, string.Join(", ", bindables.Select(b => b.Identifier)));
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
    }
}