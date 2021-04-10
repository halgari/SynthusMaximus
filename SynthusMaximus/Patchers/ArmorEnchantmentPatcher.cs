using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using SynthusMaximus.Support.RunSorting;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;
using static SynthusMaximus.Data.Statics;

namespace SynthusMaximus.Patchers
{
    
    [RunAfter(typeof(ArmorPatcher))]
    public class ArmorEnchantmentPatcher : APatcher<ArmorEnchantmentPatcher>
    {
        private int _totalAdded;

        public ArmorEnchantmentPatcher(ILogger<ArmorEnchantmentPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        public override void RunPatcher()
        {
            RunListEnchantmentBindings();
            RunDirectMatch();
            Logger.LogInformation("Generated {total} enchanted armors", _cachedArmor.Count);
        }

        private void RunListEnchantmentBindings()
        {
            var toAdd = new List<LeveledItemEntry>();
            foreach (var list in Storage.ListEnchantmentBindings)
            {
                var resolvedList = list.EdidList.Resolve(State.LinkCache);
                if (list.FillListWithSimilars)
                {
                    if (resolvedList.Entries == null) continue;
                        
                    foreach (var entry in resolvedList.Entries)
                    {
                        var resolvedEntry = entry.Data!.Reference.TryResolve<IArmorGetter>(State.LinkCache);
                        if (resolvedEntry == null) continue;
                        if (resolvedEntry.ObjectEffect.IsNull) continue;
                        if (resolvedEntry.TemplateArmor.IsNull) continue;
                        if (Storage.EnchantmentArmorExclusions.IsExcluded(resolvedEntry)) continue;

                        var template = resolvedEntry.TemplateArmor.Resolve(State.LinkCache);
                        if (!template.ObjectEffect.IsNull) continue;
                            
                        foreach (var similar in GetSimilarArmors(template).ToList())
                        {
                            if (similar.ObjectEffect.FormKey == resolvedEntry.ObjectEffect.FormKey)
                                continue;
                            var newArmor = CreateEnchantedArmorFromTemplate(template, similar,
                                new FormLink<IObjectEffectGetter>(resolvedEntry.ObjectEffect.FormKey));
                                
                            toAdd.Add(new LeveledItemEntry
                            {
                                Data = new LeveledItemEntryData
                                {
                                    Reference = new FormLink<IItemGetter>(newArmor),
                                    Count = entry.Data!.Count,
                                    Level = entry.Data!.Level
                                }
                            });
                        }

                    }
                    if (toAdd.Count == 0) continue;
                    _totalAdded += toAdd.Count;

                    var lo = Patch.LeveledItems.DuplicateInAsNewRecord(resolvedList);
                    lo.Entries!.AddRange(toAdd);
                    toAdd.Clear();

                    resolvedList = lo;
                }


                foreach (var entry in resolvedList.Entries ?? new List<ILeveledItemEntryGetter>())
                {
                    var resolvedEntry = entry.Data!.Reference.TryResolve<IArmorGetter>(State.LinkCache);
                    if (resolvedEntry == null) continue;
                    if (resolvedEntry.ObjectEffect.IsNull) continue;
                    if (resolvedEntry.TemplateArmor.IsNull) continue;
                    
                    if (Storage.EnchantmentArmorExclusions.IsExcluded(resolvedEntry)) continue;
                    var template = resolvedEntry.TemplateArmor.Resolve(State.LinkCache);
                    if (!template.ObjectEffect.IsNull) continue;

                    foreach (var replacer in list.Replacers)
                    {
                        if (resolvedEntry.ObjectEffect.FormKey != replacer.EdidBase.FormKey) continue;
                        
                        var newArmor = CreateEnchantedArmorFromTemplate(template, resolvedEntry,
                            new FormLink<IObjectEffectGetter>(replacer.EdidNew.FormKey));
                        
                        toAdd.Add(new LeveledItemEntry
                        {
                            Data = new LeveledItemEntryData
                            {
                                Reference = new FormLink<IItemGetter>(newArmor),
                                Count = entry.Data!.Count,
                                Level = entry.Data!.Level
                            }
                        });
                            
                    }

                }

                {
                    if (toAdd.Count == 0) continue;
                    _totalAdded += toAdd.Count;

                    var lo = Patch.LeveledItems.DuplicateInAsNewRecord(resolvedList);
                    lo.Entries!.AddRange(toAdd);
                    toAdd.Clear();
                }


            }
        }

        private IEnumerable<IArmorGetter> GetSimilarArmors(IArmorGetter a)
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

        private void RunDirectMatch()
        {
            foreach (var armor in Mods.Armor().WinningOverrides())
            {
                var material = Storage.GetArmorMaterial(armor);
                if (material == null) continue;
                
                if (armor.ObjectEffect.IsNull) continue;
                if (Storage.EnchantmentArmorExclusions.IsExcluded(armor)) continue;

                if (armor.TemplateArmor.IsNull) continue;

                var template = armor.TemplateArmor.Resolve(State.LinkCache);
                if (Storage.EnchantmentArmorExclusions.IsExcluded(template)) continue;
                
                
                foreach (var other in Storage.DirectEnchantmentBindings[new FormLink<IObjectEffectGetter>(armor.ObjectEffect.FormKey)])
                {
                    var newArmor = CreateEnchantedArmorFromTemplate(template, armor, other.New);
                    foreach (var lst in Mods.LeveledItem().WinningOverrides())
                    {
                        if (Storage.DistributionExclusionsArmor.IsExcluded(lst)) continue;
                        if (lst!.Flags.HasFlag(LeveledItem.Flag.UseAll)) continue;
                        
                        if (lst.Entries == null) continue;

                        var toAdd = lst.Entries
                            .Where(e => e.Data!.Reference.FormKey == armor.FormKey)
                            .Select(e => new LeveledItemEntry
                            {
                                Data = new LeveledItemEntryData()
                                {
                                    Reference = new FormLink<IItemGetter>(newArmor.FormKey),
                                    Count = e.Data!.Count,
                                    Level = e.Data.Level
                                }
                            })
                            .ToList();

                        if (toAdd.Count == 0) continue;
                        _totalAdded += toAdd.Count;
                        var lstm = Patch.LeveledItems.GetOrAddAsOverride(lst);
                        lstm.Entries!.AddRange(toAdd);
                    }
                }
            }
        }

        private Dictionary<(FormKey Template, FormKey Like, FormKey Effect), IArmorGetter> _cachedArmor = new();
        private IArmorGetter CreateEnchantedArmorFromTemplate(IArmorGetter template, IArmorGetter like, IFormLink<IObjectEffectGetter> e)
        {
            if (_cachedArmor.TryGetValue((template.FormKey, like.FormKey, e.FormKey), out var found))
                return found;
            
            var resolved = e.Resolve(State.LinkCache);
            var newArmor = Patch.Armors.DuplicateInAsNewRecord(template);
            newArmor.SetEditorID(
                SPrefixPatcher + SPrefixArmor + template.NameOrEmpty() + resolved.NameOrEmpty(), resolved);
            newArmor.TemplateArmor.SetTo(template);
            newArmor.ObjectEffect.SetTo(e);
            newArmor.Value = like.Value;
            newArmor.Name = Storage.GetLocalizedEnchantmentNameArmor(template, e);

            _cachedArmor[(template.FormKey, like.FormKey, e.FormKey)] = newArmor;
            return newArmor;
        }
    }
}