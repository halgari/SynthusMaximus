using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using System.Linq;
using SynthusMaximus.Data.DTOs;

namespace SynthusMaximus.Patchers
{
    public abstract class ADistributeItemEnchantments<TPatcher, TItem> : APatcher<TPatcher>
    where TItem: class, IItemGetter, ITranslatedNamedGetter
    {
        public ADistributeItemEnchantments(ILogger<TPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        /// <summary>
        /// Given an enchanted item, return it's template, return value can be a null link
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected abstract IFormLinkNullableGetter<TItem> GetTemplate(TItem i);
        
        /// <summary>
        /// Return the enchantment for this item, can return a null link;;
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected abstract IFormLinkNullableGetter<IEffectRecordGetter> GetEnchantment(TItem i);
        
        /// <summary>
        /// Given a template, and a existing enchanted item, create a new item with the given enchantment
        /// </summary>
        /// <param name="template"></param>
        /// <param name="like"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        protected abstract TItem CreateItemFromTemplate(TItem template, TItem like, IFormLink<IObjectEffectGetter> e);

        protected abstract MajorRecordExclusionList<ILeveledItemGetter> GetDistributionExclusionList();
        
        protected abstract ParallelQuery<TItem> AllItems();


        /// <summary>
        /// Get a exclusion list for items that shouldn't have this logic run on them.
        /// </summary>
        /// <returns></returns>
        protected abstract ExclusionList<TItem> GetEnchantmentExclusionList();

        protected override void RunPatcherInner()
        {
            RunListEnchantmentBindings();
            RunDirectMatch();
        }

        private void RunListEnchantmentBindings()
        {
            var query = from binding in Storage.ListEnchantmentBindings.AsParallel()
                let resolvedList = binding.EdidList.Resolve(LinkCache)
                from entry in resolvedList.Entries.EmptyIfNull()
                let resolvedEntry = entry.Data!.Reference.TryResolve<TItem>(LinkCache)
                where resolvedEntry != null
                where !GetEnchantment(resolvedEntry).IsNull
                where !GetTemplate(resolvedEntry).IsNull
                where !GetEnchantmentExclusionList().Matches(resolvedEntry)
                let template = GetTemplate(resolvedEntry).Resolve(LinkCache)
                where GetEnchantment(template).IsNull
                from replacer in binding.Replacers
                where GetEnchantment(resolvedEntry).FormKey == replacer.EdidBase.FormKey
                group (replacer.EdidNew, template, resolvedEntry, entry, resolvedList) by resolvedList;

            var results = query.ToList();

            foreach (var listGroup in results)
            {
                var lo = Patch.LeveledItems.GetOrAddAsOverride(listGroup.Key);
                foreach (var entry in listGroup)
                {
                    var newArmor = CreateItemFromTemplate(entry.template, entry.resolvedEntry!,
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
            Logger.LogInformation("Added {count} variants of enchanted {type}", results.Count, typeof(TItem).Name);
        }


        private void RunDirectMatch()
        {
            var query = from item in AllItems().AsParallel()
                where !GetEnchantment(item).IsNull
                where !GetEnchantmentExclusionList().Matches(item)
                where !GetTemplate(item).IsNull
                let template = GetTemplate(item).Resolve(LinkCache)
                where !GetEnchantmentExclusionList().Matches(template)
                from other in Storage.DirectEnchantmentBindings[
                    new FormLink<IObjectEffectGetter>(GetEnchantment(item).FormKey)]
                from list in Mods.LeveledItem().WinningOverrides().AsParallel()
                where !GetDistributionExclusionList().Matches(list)
                where !list.Flags.HasFlag(LeveledItem.Flag.UseAll)
                where list.Entries != null
                from entry in list.Entries
                where entry.Data.Reference.FormKey == item.FormKey
                select (list, template, item, other.New, entry);

            var results = query.ToList();

            var newItems = results.GroupBy(a => (a.template.FormKey, a.item.FormKey, a.New.FormKey))
                .ToDictionary(a => a.Key, a =>
                {
                    var f = a.First();
                    return CreateItemFromTemplate(f.template, f.item, f.New);
                });

            foreach (var listGroup in results.GroupBy(g => g.list))
            {
                var lo = Patch.LeveledItems.GetOrAddAsOverride(listGroup.Key);
                foreach (var e in listGroup)
                {
                    var newItem = newItems[(e.template.FormKey, e.item.FormKey, e.New.FormKey)];
                    lo.Entries!.Add(new LeveledItemEntry
                    {
                        Data = new LeveledItemEntryData()
                        {
                            Reference = new FormLink<IItemGetter>(newItem.FormKey),
                            Count = e.entry.Data!.Count,
                            Level = e.entry.Data.Level
                        }
                    });;
                    Success(newItem, listGroup.Key);
                }
            }
            
            Logger.LogInformation("Added {count} new enchanted {type} variants from direct bindings", results.Count, typeof(TItem).Name);
        }
    }
}