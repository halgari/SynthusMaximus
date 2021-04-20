using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Perk;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Perk;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Spell;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Perk;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Npc;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Spell;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.MiscItem;

namespace SynthusMaximus.Patchers
{
    public class NPCPatcher : APatcher<NPCPatcher>
    {
        public NPCPatcher(ILogger<NPCPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        protected override void RunPatcherInner()
        {
            PatchPlayer();
            foreach (var npc in Mods.Npc().WinningOverrides())
            {
                if (!ShouldPatch(npc)) continue;
                
                var nnpc = Patch.Npcs.GetOrAddAsOverride(npc);

                if (Storage.UseMage)
                {
                    nnpc.AddPerk(xMAMAGPassiveScalingSpells);
                    nnpc.AddPerk(xMAMAGPassiveEffects);
                    nnpc.AddPerk(AlchemySkillBoosts);
                }

                if (Storage.UseThief)
                {
                    nnpc.AddSpell(xMATHICombatAbility);
                }

                if (Storage.UseWarrior)
                {
                    nnpc.AddPerk(xMAHEWScarredPassive);
                    nnpc.AddPerk(xMAWARPassiveScalingFistWeapon);
                    nnpc.AddSpell(xMAWARShieldTypeDetectorAbility);
                    nnpc.AddPerk(xMAWARPassiveScalingCriticalDamage);
                    nnpc.AddPerk(xMAWARPassiveCrossbowEffects);
                }
            }
        }

        private bool ShouldPatch(INpcGetter npc)
        {
            return !Storage.NPCExclusions.IsExcluded(npc);
        }

        private void PatchPlayer()
        {
            var player = Patch.Npcs.GetOrAddAsOverride(Player, State.LinkCache);
            player.AddSpell(xMAWeaponSpeedFix);

            if (Storage.UseMage)
            {
                player.RemoveSpell(Flames);
                player.RemoveSpell(Healing);
                player.AddSpell(xMAMAGMainAbility);
                player.AddPerk(xMAMAGPassiveScalingSpellsScroll);

                if (!Storage.ShouldRemoveUnspecificSpells)
                {
                    player.AddSpell(xMADESFireFlames);
                    player.AddSpell(xMARESHealRecovery);
                }
            }

            if (Storage.UseThief)
            {
                player.AddPerk(xMATHIPassiveLockpickingXP);
                player.AddPerk(xMATHIPassiveSpellSneakScaling);
                player.AddPerk(xMATHIPassiveArmorSneakPenalty);
                player.AddSpell(xMATHIMainAbility);
                player.AddSpell(xMATHIInitSneakTools);
                player.AddPerk(xMATHIPassiveShoutScaling);
            }

            if (Storage.UseWarrior)
            {
                player.AddSpell(xMAWARTimedBlockingAbility);
                player.AddPerk(ArcaneBlacksmith);
                player.AddPerk(xMAWARPassiveDualWieldMalus);

            }

        }
    }
}