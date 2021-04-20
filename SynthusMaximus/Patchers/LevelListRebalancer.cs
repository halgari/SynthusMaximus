using System;
using System.Linq;
using DynamicData;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using SynthusMaximus.Support.RunSorting;

namespace SynthusMaximus.Patchers
{
    /// <summary>
    /// Some item level lists end up with more than 256 entries, this breaks those lists into acceptable sizes
    /// </summary>
    [RunAtEnd]
    public class LevelListRebalancer : APatcher<LevelListRebalancer>
    {
        public LevelListRebalancer(ILogger<LevelListRebalancer> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        private const int MAX_SIZE = 254;

        protected override void RunPatcherInner()
        {
            var lists = Mods.LeveledItem().WinningOverrides()
                .Where(lst => (lst.Entries?.Count ?? 0) > MAX_SIZE)
                .Select(lst =>
                    new
                    {
                        List = lst,
                        Groups = lst.Entries!
                            .Select((e, idx) => (e, idx))
                            .GroupBy(t => t.idx / MAX_SIZE, t => t.e.DeepCopy())
                            .ToList()
                    })
                .ToList();

            foreach (var list in lists)
            {
                var lo = Patch.LeveledItems.GetOrAddAsOverride(list.List);
                lo.Entries!.Clear();
                foreach (var group in list.Groups)
                {
                    var copy = Patch.LeveledItems.DuplicateInAsNewRecord(list.List);
                    copy.EditorID = copy.EditorID + "_sublist_" + group.Key; 
                    copy.Entries = new ExtendedList<LeveledItemEntry>();
                    copy.Entries.AddRange(group);
                    lo.Entries.Add(new LeveledItemEntry
                    {
                        Data = new LeveledItemEntryData()
                        {
                            Reference = new FormLink<IItemGetter>(copy),
                            Level = copy.Entries.Min(e => e.Data!.Level),
                            Count = 1,
                        }
                    });
                }
            }

        }
    }
}