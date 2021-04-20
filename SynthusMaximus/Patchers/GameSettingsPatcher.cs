using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;

namespace SynthusMaximus.Patchers
{
    public class GameSettingsPatcher : APatcher<GameSettingsPatcher>
    {
        public GameSettingsPatcher(ILogger<GameSettingsPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        protected override void RunPatcherInner()
        {
            foreach (var g in Mods.GameSetting().WinningOverrides())
            {
                if (Storage.UseWarrior)
                {
                    switch (g.EditorID)
                    {
                        case Statics.GmstfArmorScalingFactor:
                            SetFloat(g, Storage.ArmorSettings.ProtectionPerArmor);
                            break;
                        case Statics.GmstfMaxArmorRating:
                            SetFloat(g, Storage.ArmorSettings.MaxProtection);
                            break;
                        case Statics.GmstfArmorRatingMax:
                            SetFloat(g, Storage.ArmorSettings.ArmorRatingMax);
                            break;
                        case Statics.GmstfArmorRatingPcMax:
                            SetFloat(g, Storage.ArmorSettings.ArmorRatingPCMax);
                            break;
                    }
                }
            }
        }

        private void SetFloat(IGameSettingGetter gs, float value)
        {
            var r = (IGameSettingFloat)Patch.GameSettings.GetOrAddAsOverride(gs);
            r.Data = value;
        }
    }
}