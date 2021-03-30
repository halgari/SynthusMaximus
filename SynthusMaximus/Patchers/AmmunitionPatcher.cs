using System;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;

namespace SynthusMaximus.Patchers
{
    public class AmmunitionPatcher : APatcher<AmmunitionPatcher>
    {
        public AmmunitionPatcher(ILogger<AmmunitionPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        public override void RunPatcher()
        {
            foreach (var ammo in Mods.Ammunition().WinningOverrides())
            {
                try
                {
                    if (!ShouldPatch(ammo))
                        continue;

                    var at = Storage.GetAmmunitionType(ammo);
                    

                }
                catch (Exception ex)
                {
                    ReportFailed(ex, ammo);
                }
            }
            
        }

        private static bool ShouldPatch(IAmmunitionGetter ammo)
        {
            if (string.IsNullOrEmpty(ammo.NameOrEmpty()))
                return false;

            return !ammo.Flags.HasFlag(Ammunition.Flag.NonPlayable);
        }
    }
}