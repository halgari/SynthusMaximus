using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;
using Wabbajack.Common;

namespace SynthusMaximus.Patchers
{
    public abstract class APatcher<TInner> : IPatcher
    {
        private enum TrackingResult
        {
            Success = 0,
            Ignored,
            Failed,
        }
        
        protected readonly ILogger<TInner> Logger;
        protected readonly DataStorage Storage;
        protected readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> State;
        protected readonly ISkyrimMod Patch;
        protected readonly IEnumerable<IModListing<ISkyrimModGetter>> Mods;
        protected readonly IEnumerable<IModListing<ISkyrimModGetter>> UnpatchedMods;
        private Dictionary<TrackingResult, List<(IMajorRecordGetter Record, string Reason)>> _trackingData = new();
        private Stopwatch _stopWatch;

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
            _stopWatch = new Stopwatch();
        }

        public ILinkCache<ISkyrimMod, ISkyrimModGetter> LinkCache { get; set; }


        protected abstract void RunPatcherInner();

        public void RunPatcher()
        {
            lock (_trackingData)
            {
                _trackingData.Clear();
                _trackingData[TrackingResult.Failed] = new List<(IMajorRecordGetter Record, string Reason)>();
                _trackingData[TrackingResult.Ignored] = new List<(IMajorRecordGetter Record, string Reason)>();
                _trackingData[TrackingResult.Success] = new List<(IMajorRecordGetter Record, string Reason)>();
            }

            Logger.LogInformation("Starting {Name}", GetType().Name);
            _stopWatch.Restart();
            RunPatcherInner();
            _stopWatch.Stop();

            WriteReports();
        }

        private void WriteReports()
        {
            lock (_trackingData)
            {
                Logger.LogInformation("Finished {Name} in {Ms}ms: {Failed} Failed, {Ignored} Ignored, {Success} Success",
                    GetType().Name, _stopWatch.ElapsedMilliseconds,
                    _trackingData[TrackingResult.Failed].GroupBy(r => r.Record.FormKey).Count(),
                    _trackingData[TrackingResult.Ignored].GroupBy(r => r.Record.FormKey).Count(),
                    _trackingData[TrackingResult.Success].GroupBy(r => r.Record.FormKey).Count());
                foreach (var (result, values) in _trackingData)
                {
                    var filename = AbsolutePath.EntryPoint.Combine("logs", GetType().Name + "_" + result + ".log");
                    var lines = values.OrderBy(v => (v.Record.FormKey.ModKey.FileName, v.Record.FormKey.ID))
                        .Select(v => $"{v.Record.FormKey} - {v.Record.EditorID} - {v.Reason}")
                        .ToArray();
                    filename.WriteAllLinesAsync(lines).Wait();
                }
            }
        }


        protected void Failed(Exception exception, IMajorRecordGetter r)
        {
            lock(_trackingData)
                _trackingData[TrackingResult.Failed].Add((r, exception.ToString()));
        }

        protected void Ignore(IMajorRecordGetter r, string reason)
        {
            lock (_trackingData)
                _trackingData[TrackingResult.Ignored].Add((r, reason));
        }
        
        protected void Success(IMajorRecordGetter r)
        {
            lock (_trackingData)
                _trackingData[TrackingResult.Success].Add((r, ""));
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