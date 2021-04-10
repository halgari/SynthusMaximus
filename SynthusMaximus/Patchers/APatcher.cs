using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;

namespace SynthusMaximus.Patchers
{
    public abstract class APatcher<TInner> : IPatcher
    {
        protected readonly ILogger<TInner> Logger;
        protected readonly DataStorage Storage;
        protected readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> State;
        protected readonly ISkyrimMod Patch;
        protected readonly IEnumerable<IModListing<ISkyrimModGetter>> Mods;
        protected readonly IEnumerable<IModListing<ISkyrimModGetter>> UnpatchedMods;

        protected APatcher(ILogger<TInner> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Logger = logger;
            Storage = storage;
            State = state;
            Patch = State.PatchMod;
            Mods = State.LoadOrder.PriorityOrder;
            LinkCache = State.LinkCache;
            UnpatchedMods = State.LoadOrder.PriorityOrder.Skip(1);
            Logger.LogInformation("Initialized");
        }

        public ILinkCache<ISkyrimMod, ISkyrimModGetter> LinkCache { get; set; }


        public abstract void RunPatcher();


        protected void ReportFailed(Exception exception, IMajorRecordGetter r)
        {
            Logger.LogError(exception, "Failed processing {EditorID}", r.EditorID);
        }

        protected void SkipRecord(IMajorRecordGetter r, string reason)
        {
            Logger.LogInformation("Skipping {EditorID}: {reason}", r.EditorID, reason);
        }

        public class IndexedEntry<T>
        where T : class, IMajorRecordGetter
        {
            public ILeveledItemGetter List { get; set; }
            public ILeveledItemEntryGetter Resolved { get; set; }
            public T Item { get; set; }

            public IndexedEntry(ILeveledItemGetter list, ILeveledItemEntryGetter entry, T item)
            {
                Item = item;
                Resolved = entry;
                List = list;
            }
        }
        
        public Dictionary<TK, IEnumerable<IndexedEntry<TS>>> IndexLeveledLists<TS, TK>(Func<TS, TK> indexFn)
        where TS : class, IMajorRecordGetter
        {
            var records = Mods.LeveledItem().WinningOverrides()
                .AsParallel()
                .Where(lst => lst.Entries != null)
                .SelectMany(lst => lst.Entries!.Select(entry => (lst, entry)))
                .Where(t => t.entry.Data != null)
                .Select(t => (t.lst, t.entry, t.entry.Data!.Reference.TryResolve<TS>(State.LinkCache)))
                .Where(t => t.Item3 != default)
                .Select(t => new IndexedEntry<TS>(t.lst, t.entry, t.Item3!))
                .ToList()
                .GroupBy(t => indexFn(t.Item))
                .ToDictionary(t => t.Key, t => (IEnumerable<IndexedEntry<TS>>)t);
            return records;
        }
        

    }
}