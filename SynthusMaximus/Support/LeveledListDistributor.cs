using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data.DTOs;
using Wabbajack.Common;
using IItemGetter = Mutagen.Bethesda.Skyrim.IItemGetter;
using ILeveledItem = Mutagen.Bethesda.Skyrim.ILeveledItem;
using ILeveledItemEntryGetter = Mutagen.Bethesda.Skyrim.ILeveledItemEntryGetter;
using ILeveledItemGetter = Mutagen.Bethesda.Skyrim.ILeveledItemGetter;
using LeveledItem = Mutagen.Bethesda.Skyrim.LeveledItem;

namespace SynthusMaximus.Support
{
    public static class LeveledListDistributorFactory
    {

        public static LeveledListDistributor<TItem, TKey> Create<TItem, TKey>(Func<TItem, TKey> indexer, 
            IPatcherState<ISkyrimMod, ISkyrimModGetter> state, MajorRecordExclusionList<ILeveledItemGetter>? listExclusions = null)
            where TKey : struct
            where TItem : IItemGetter
        {
            return new(indexer, state, listExclusions);
        }
        
    }

    public class LeveledListDistributor<TItem, TKey>
        where TKey : struct
        where TItem : IItemGetter
    {
        private Func<TItem, TKey> _indexer;
        private IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
        private MajorRecordExclusionList<ILeveledItemGetter> _listExclusions;
        private HashSet<(FormKey List, FormKey Item)> _inList;
        private Dictionary<TKey, IEnumerable<(ILeveledItemGetter List, ILeveledItemEntryGetter Entry, TKey Key, IItemGetter Item)>> _items;

        public LeveledListDistributor(Func<TItem, TKey> indexer, 
            IPatcherState<ISkyrimMod, ISkyrimModGetter> state, MajorRecordExclusionList<ILeveledItemGetter>? listExclusions = null)
        {
            _indexer = indexer;
            _listExclusions = listExclusions ?? new MajorRecordExclusionList<ILeveledItemGetter>();
            _state = state;
        }

        public void Index()
        { 
            _items = _state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides()
                .AsParallel()
                .Where(lst => !_listExclusions.IsExcluded(lst))
                .SelectMany(lst => lst.Entries!.Select(e => (lst, e, e.Data!.Reference.TryResolve(_state.LinkCache))))
                .Where(t => t.Item3 is TItem)
                .Select(t => (t.lst, t.e, _indexer((TItem) t.Item3!), t.Item3!))
                .AsEnumerable()
                .GroupBy(t => t.Item3)
                .ToDictionary(t => t.Key, t => t.DistinctBy(f => f.lst.FormKey));

            _inList = _items.SelectMany(i => i.Value.Select(v => (v.List.FormKey, v.Entry.Data!.Reference.FormKey))).ToHashSet();
        }

        public void Distribute(TItem newItem)
        {
            var newKey = _indexer(newItem);

            if (!_items.TryGetValue(newKey, out var found))
                return;

            foreach (var entry in found)
            {
                if (_inList.Contains((entry.List.FormKey, newItem.FormKey)))
                    continue;

                var me = _state.PatchMod.LeveledItems.GetOrAddAsOverride(entry.List);
                me.Entries!.Add(new LeveledItemEntry()
                {
                    Data = new LeveledItemEntryData()
                    {
                        Reference = new FormLink<IItemGetter>(newItem.FormKey),
                        Level = entry.Entry.Data!.Level,
                        Count = entry.Entry.Data!.Count
                    }
                });
                _inList.Add((entry.List.FormKey, newItem.FormKey));
            }
        }


    }
}