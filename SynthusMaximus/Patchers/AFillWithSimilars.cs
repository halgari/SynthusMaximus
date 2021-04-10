using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;
using System.Linq;
using Noggog;
using SynthusMaximus.Data.DTOs;

namespace SynthusMaximus.Patchers
{
    /// <summary>
    /// Abstract class for easily filling leveled lists with similar items
    /// "similar" is defined via set of abstract methods.
    /// </summary>
    /// <typeparam name="TPatcher"></typeparam>
    /// <typeparam name="TItem"></typeparam>
    public abstract class AFillWithSimilars<TPatcher, TItem> : APatcher<TPatcher>
    where TItem : class, IItemGetter, ITranslatedNamedGetter
    {
        protected AFillWithSimilars(ILogger<TPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
            
        }

        /// <summary>
        /// Given an Item, return a list of similar items (excluding this item)
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected abstract IEnumerable<TItem> GetSimilars(TItem i);
        
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

        
        /// <summary>
        /// Get a exclusion list for items that shouldn't have this logic run on them.
        /// </summary>
        /// <returns></returns>
        protected abstract ExclusionList<TItem> GetEnchantmentExclusionList(); 
        
        
        public override void RunPatcher()
        {
            var query =
                from binding in Storage.ListEnchantmentBindings
                where binding.FillListWithSimilars
                let list = binding.EdidList.Resolve(LinkCache)
                from listEntry in list.Entries.EmptyIfNull()
                let resolved = listEntry.Data.Reference.TryResolve<TItem>(LinkCache)
                where resolved != null
                where !GetEnchantment(resolved).IsNull
                where !GetTemplate(resolved).IsNull
                where !GetEnchantmentExclusionList().IsExcluded(resolved)
                let parentTemplate = GetTemplate(resolved).Resolve(LinkCache)
                where GetEnchantment(parentTemplate).IsNull
                where binding.Replacers.Any(r => r.EdidBase.FormKey == GetEnchantment(resolved).FormKey)
                from similar in GetSimilars(parentTemplate)
                group (list, similar, resolved, parentTemplate) by (list, similar, listEntry.Data.Level, listEntry.Data.Count)
                into grouped
                    select (grouped.Key.list, grouped.Key.similar, grouped.Key.Count, grouped.Key.Level, grouped.First().resolved, grouped.First().parentTemplate);

            var results = query.ToList();

            foreach (var t in results.GroupBy(t => t.list))
            {
                var lo = Patch.LeveledItems.GetOrAddAsOverride(t.Key);
                foreach (var entry in t)
                {
                    var newItem = CreateItemFromTemplate(entry.parentTemplate, entry.similar,
                        new FormLink<IObjectEffectGetter>(GetEnchantment(entry.resolved!).FormKey));

                    lo.Entries!.Add(new LeveledItemEntry
                    {
                        Data = new LeveledItemEntryData
                        {
                            Reference = new FormLink<IItemGetter>(newItem),
                            Count = entry.Count,
                            Level = entry.Level
                        }
                    });
                }
            }
            Logger.LogInformation("Found {results} similar {item} to insert", results.Count(), typeof(TItem).Name);
            
        }
    }
}