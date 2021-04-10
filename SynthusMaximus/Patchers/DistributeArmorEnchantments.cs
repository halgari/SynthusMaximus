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
    [RunAfter(typeof(FillArmorListsWithSimilars))]
    public class DistributeArmorEnchantments : APatcher<DistributeArmorEnchantments>
    {
        public DistributeArmorEnchantments(ILogger<DistributeArmorEnchantments> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        public override void RunPatcher()
        {
            RunListEnchantmentBindings();
            RunDirectMatch();
        }

        private void RunListEnchantmentBindings()
        {
            var query = from binding in Storage.ListEnchantmentBindings.AsParallel()
                let resolvedList = binding.EdidList.Resolve(LinkCache)
                from entry in resolvedList.Entries.EmptyIfNull()
                let resolvedEntry = entry.Data!.Reference.TryResolve<IArmorGetter>(LinkCache)
                where resolvedEntry != null
                where !resolvedEntry.ObjectEffect.IsNull
                where !resolvedEntry.TemplateArmor.IsNull
                where !Storage.EnchantmentArmorExclusions.IsExcluded(resolvedEntry)
                let template = resolvedEntry.TemplateArmor.Resolve(LinkCache)
                where template.ObjectEffect.IsNull
                from replacer in binding.Replacers
                where resolvedEntry.ObjectEffect.FormKey == replacer.EdidBase.FormKey
                group (replacer.EdidNew, template, resolvedEntry, entry, resolvedList) by resolvedList;

            var results = query.ToList();

            foreach (var listGroup in results)
            {
                var lo = Patch.LeveledItems.GetOrAddAsOverride(listGroup.Key);
                foreach (var entry in listGroup)
                {
                    var newArmor = CreateEnchantedArmorFromTemplate(entry.template, entry.resolvedEntry!,
                        new FormLink<IObjectEffectGetter>(entry.EdidNew.FormKey));
                    lo.Entries!.Add(new LeveledItemEntry
                    {
                        Data = new LeveledItemEntryData
                        {
                            Reference = new FormLink<IItemGetter>(newArmor),
                            Count = entry.entry.Data!.Count,
                            Level = entry.entry.Data!.Level
                        }
                    });
                }
            }
            Logger.LogInformation("Added {count} variants of enchanted armors", results.Count);
        }


        private void RunDirectMatch()
        {
            var query = from armor in Mods.Armor().WinningOverrides().AsParallel()
                let material = Storage.GetArmorMaterial(armor)
                where material != null
                where !armor.ObjectEffect.IsNull
                where !Storage.EnchantmentArmorExclusions.IsExcluded(armor)
                where !armor.TemplateArmor.IsNull
                let template = armor.TemplateArmor.Resolve(LinkCache)
                where !Storage.EnchantmentArmorExclusions.IsExcluded(template)
                from other in Storage.DirectEnchantmentBindings[
                    new FormLink<IObjectEffectGetter>(armor.ObjectEffect.FormKey)]
                from list in Mods.LeveledItem().WinningOverrides()
                where !Storage.DistributionExclusionsArmor.IsExcluded(list)
                where !list.Flags.HasFlag(LeveledItem.Flag.UseAll)
                where list.Entries != null
                from entry in list.Entries
                where entry.Data.Reference.FormKey == armor.FormKey
                select (list, template, armor, other.New, entry);

            var results = query.ToList();

            var newArmors = results.GroupBy(a => (a.template, a.armor, a.New))
                .ToDictionary(a => a.Key, a =>
                {
                    var f = a.First();
                    return CreateEnchantedArmorFromTemplate(f.template, f.armor, f.New);
                });

            foreach (var listGroup in results.GroupBy(g => g.list))
            {
                var lo = Patch.LeveledItems.GetOrAddAsOverride(listGroup.Key);
                foreach (var e in listGroup)
                {
                    var newArmor = newArmors[(e.template, e.armor, e.New)];
                    lo.Entries!.Add(new LeveledItemEntry
                    {
                        Data = new LeveledItemEntryData()
                        {
                            Reference = new FormLink<IItemGetter>(newArmor.FormKey),
                            Count = e.entry.Data!.Count,
                            Level = e.entry.Data.Level
                        }
                    });

                }
            }
            
            Logger.LogInformation("Added {count} new enchanted armor variants from direct bindings", results.Count);
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