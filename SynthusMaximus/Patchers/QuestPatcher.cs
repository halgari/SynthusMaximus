using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;
using Wabbajack.Common;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Quest;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Spell;

namespace SynthusMaximus.Patchers
{
    public class QuestPatcher : APatcher<QuestPatcher>
    {
        public QuestPatcher(ILogger<QuestPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        public override void RunPatcher()
        {
            if (Storage.UseMage)
            {
                var qest = Patch.Quests.GetOrAddAsOverride(MQ101.Resolve(State.LinkCache));
                var package = qest.VirtualMachineAdapter!.GetOrAddScript("MQ101QuestScript");
                package.Properties.Where(p => p.Name == "Fury")
                    .OfType<ScriptObjectProperty>()
                    .Do(p => {p.Object.SetTo(xMAILLInfluenceFear);});
                package.Properties.Where(p => p.Name == "ConjureFamiliar")
                    .OfType<ScriptObjectProperty>()
                    .Do(p => {p.Object.SetTo(xMACONConjureWeakFlameAtronach);});
                package.Properties.Where(p => p.Name == "Sparks")
                    .OfType<ScriptObjectProperty>()
                    .Do(p => {p.Object.SetTo(xMADESShockSparks);});
            }
        }
    }
}