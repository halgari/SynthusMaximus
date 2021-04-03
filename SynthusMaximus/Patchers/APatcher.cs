using System;
using System.Collections.Generic;
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

        protected APatcher(ILogger<TInner> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Logger = logger;
            Storage = storage;
            State = state;
            Patch = State.PatchMod;
            Mods = State.LoadOrder.PriorityOrder;
            Logger.LogInformation("Initialized");
        }



        public abstract void RunPatcher();


        protected void ReportFailed(Exception exception, IMajorRecordGetter r)
        {
            Logger.LogError(exception, "Failed processing {EditorID}", r.EditorID);
        }

        protected void SkipRecord(IMajorRecordGetter r, string reason)
        {
            Logger.LogInformation("Skipping {EditorID}: {reason}", r.EditorID, reason);
        }
        

    }
}