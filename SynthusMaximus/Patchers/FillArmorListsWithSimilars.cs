using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using SynthusMaximus.Support;
using SynthusMaximus.Support.RunSorting;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;
using static SynthusMaximus.Data.Statics;

namespace SynthusMaximus.Patchers
{
    [RunAfter(typeof(ArmorPatcher))]
    public class FillArmorListsWithSimilars : APatcher<FillArmorListsWithSimilars>
    {
        private Eager<ILookup<IArmorGetter, List<IArmorGetter>>> _similars;
        
        public FillArmorListsWithSimilars(ILogger<FillArmorListsWithSimilars> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }
        
        public override void RunPatcher()
        {
            var query =
                from binding in Storage.ListEnchantmentBindings
                where binding.FillListWithSimilars
                let list = binding.EdidList.Resolve(LinkCache)
                from listEntry in list.Entries.EmptyIfNull()
                let resolved = listEntry.Data.Reference.TryResolve<IArmorGetter>(LinkCache)
                where resolved != null
                where !resolved.ObjectEffect.IsNull
                where !resolved.TemplateArmor.IsNull
                where Storage.EnchantmentArmorExclusions.IsExcluded(resolved)
                let parentTemplate = resolved.TemplateArmor.Resolve(LinkCache)
                where parentTemplate.ObjectEffect.IsNull
                where binding.Replacers.Any(r => r.EdidBase.FormKey == resolved.ObjectEffect.FormKey) 
                from similar in GetSimilarArmors(parentTemplate)
                group (list, similar, resolved, parentTemplate) by (list, similar, listEntry.Data.Level, listEntry.Data.Count)
                into grouped
                    select (grouped.Key.list, grouped.Key.similar, grouped.Key.Count, grouped.Key.Level, grouped.First().resolved, grouped.First().parentTemplate);

            var results = query.ToList();

            foreach (var t in results.GroupBy(t => t.list))
            {
                var lo = Patch.LeveledItems.GetOrAddAsOverride(t.Key);
                foreach (var entry in t)
                {
                    var newArmor = CreateEnchantedArmorFromTemplate(entry.parentTemplate, entry.similar,
                        new FormLink<IObjectEffectGetter>(entry.resolved!.ObjectEffect.FormKey));

                    lo.Entries!.Add(new LeveledItemEntry
                    {
                        Data = new LeveledItemEntryData
                        {
                            Reference = new FormLink<IItemGetter>(newArmor),
                            Count = entry.Count,
                            Level = entry.Level
                        }
                    });
                }
            }
            Logger.LogInformation("Found {results} similar armors to insert", results.Count());
            
        }
        

        private IArmorGetter CreateEnchantedArmorFromTemplate(IArmorGetter template, IArmorGetter like, IFormLink<IObjectEffectGetter> e)
        {

            var resolved = e.Resolve(State.LinkCache);
            var newArmor = Patch.Armors.DuplicateInAsNewRecord(template);
            newArmor.SetEditorID(
                SPrefixPatcher + SPrefixArmor + template.NameOrEmpty() + resolved.NameOrEmpty(), resolved);
            newArmor.TemplateArmor.SetTo(template);
            newArmor.ObjectEffect.SetTo(e);
            newArmor.Value = like.Value;
            newArmor.Name = Storage.GetLocalizedEnchantmentNameArmor(template, e);
            return newArmor;
        }
        
        public IEnumerable<IArmorGetter> GetSimilarArmors(IArmorGetter a)
        {
            if (Storage.IsClothing(a))
            {
                foreach (var ae in Mods.Armor().WinningOverrides())
                {
                    if (ae.FormKey == a.FormKey) continue;
                    if (Storage.IsClothing(ae) && 
                        ae.ObjectEffect.IsNull &&
                        AreClothingPiecesSimilar(a, ae))
                    {
                        if (Storage.CanArmorNotBeSimilar(a, ae)) continue;
                        yield return ae;
                    }
                }
            }
            else if (Storage.IsJewelry(a))
            {
                foreach (var ae in Mods.Armor().WinningOverrides())
                {
                    if (ae.FormKey == a.FormKey) continue;
                    if (Storage.IsJewelry(ae) &&
                        ae.ObjectEffect.IsNull &&
                        AreJewelryPiecesSimilar(a, ae))
                    {
                        if (Storage.CanArmorNotBeSimilar(a, ae)) continue;
                        yield return ae;

                    }
                }
            }
            else
            {
                foreach (var ae in Mods.Armor().WinningOverrides())
                {
                    if (ae.FormKey == a.FormKey) continue;
                    if (!Storage.IsClothing(ae) &&
                        !Storage.IsJewelry(ae) &&
                        !ae.ObjectEffect.IsNull &&
                        AreArmorPiecesSimilar(a, ae))
                    {
                        if (Storage.CanArmorNotBeSimilar(a, ae)) continue;
                        yield return ae;
                    }
                }
            }
        }

        private bool AreArmorPiecesSimilar(IArmorGetter a, IArmorGetter b)
        {
            var ama = Storage.GetArmorMaterial(a);
            var amb = Storage.GetArmorMaterial(b);
            if (ama == null || amb == null) return false;

            return DoClothingPicesHaveSameSlot(a, b) &&
                   DoArmorPiecesHaveSameType(a, b) &&
                ama.Type.Data?.BreakdownProduct?.FormKey == amb.Type.Data?.BreakdownProduct?.FormKey &&
                ama.Type.Data?.TemperingInput?.FormKey == amb.Type.Data?.TemperingInput?.FormKey;
        }

        private bool DoArmorPiecesHaveSameType(IArmorGetter a, IArmorGetter b)
        {
            return a.HasKeyword(ArmorHeavy) && b.HasKeyword(ArmorHeavy) ||
                   a.HasKeyword(ArmorLight) && b.HasKeyword(ArmorHeavy) ||
                   !a.HasKeyword(ArmorLight) && !a.HasKeyword(ArmorHeavy) &&
                   !b.HasKeyword(ArmorLight) && !b.HasKeyword(ArmorHeavy);
        }

        private bool AreJewelryPiecesSimilar(IArmorGetter a, IArmorGetter b)
        {
            return DoClothingPicesHaveSameSlot(a, b) && DoJewelryPiecesHaveSimilarPriceCategory(a, b);
        }

        private bool DoJewelryPiecesHaveSimilarPriceCategory(IArmorGetter a, IArmorGetter b)
        {
            return a.HasKeyword(JewelryExpensive) && b.HasKeyword(JewelryExpensive) ||
                   !a.HasKeyword(JewelryExpensive) && !b.HasKeyword(JewelryExpensive);
        }

        private bool AreClothingPiecesSimilar(IArmorGetter a, IArmorGetter b)
        {
            return DoClothingPicesHaveSameSlot(a, b) && DoClothingPiecesHaveSimilarPriceCategory(a, b);
        }

        private bool DoClothingPiecesHaveSimilarPriceCategory(IArmorGetter a, IArmorGetter b)
        {
            return a.HasKeyword(ClothingPoor) && b.HasKeyword(ClothingPoor) ||
                   a.HasKeyword(ClothingRich) && b.HasKeyword(ClothingRich) ||
                   !a.HasKeyword(ClothingPoor) && !b.HasKeyword(ClothingRich) ||
                   !a.HasKeyword(ClothingRich) && !b.HasKeyword(ClothingPoor);
        }

        private static List<IFormLink<IKeywordGetter>> _clothingBodySlots = new()
        {
            ClothingCirclet,
            ClothingFeet,
            ClothingBody,
            ClothingHead,
            ClothingFeet,
            ClothingNecklace,
            ClothingRing,
            ArmorBoots,
            ArmorCuirass,
            ArmorHelmet,
            ArmorShield,
            ArmorGauntlets
        };


        private bool DoClothingPicesHaveSameSlot(IArmorGetter a, IArmorGetter b)
        {
            var empty = Array.Empty<IFormLink<IKeywordGetter>>();
            var matching = a.Keywords?.Union(b.Keywords?.Union(_clothingBodySlots) ?? empty) ?? empty;
            return matching.Any();
        }


    }
}