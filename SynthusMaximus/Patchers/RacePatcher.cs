using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Spell;

namespace SynthusMaximus.Patchers
{
    public class RacePatcher : APatcher<RacePatcher>
    {
        public RacePatcher(ILogger<RacePatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        protected override void RunPatcherInner()
        {
            foreach (var race in Mods.Race().WinningOverrides())
            {
                if (Storage.RaceExclusions.IsExcluded(race))
                    continue;

                var ro = Patch.Races.GetOrAddAsOverride(race);
                if (Storage.UseWarrior)
                {
                    ro.AddSpell(xMAWARMainLogicAbility);
                    ro.AddSpell(xMAWARMainStaminaAbility);
                }

                if (Storage.UseThief && Storage.UseWarrior && ro.Flags.HasFlag(Race.Flag.Playable))
                {
                    ro.AddSpell(xMATHICombatAbility);
                    ro.AddSpell(xMAWARTHIPassiveArmorHeavy);
                    ro.AddSpell(xMAWARTHIPassiveArmorLight);
                }
            }
        }
    }
}