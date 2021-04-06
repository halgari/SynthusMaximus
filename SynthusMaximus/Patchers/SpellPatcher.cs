using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;
using SynthusMaximus.Support;
using ActorValue = Mutagen.Bethesda.Skyrim.ActorValue;
using ISpellGetter = Mutagen.Bethesda.Skyrim.ISpellGetter;

namespace SynthusMaximus.Patchers
{
    public class SpellPatcher : APatcher<SpellPatcher>
    {
        public SpellPatcher(ILogger<SpellPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        public override void RunPatcher()
        {
            if (!Storage.UseMage)
                return;
            foreach (var spell in UnpatchedMods.Spell().WinningOverrides())
            {
                DisableAssociatedMagicSchools(spell);
            }
            
            foreach (var ench in UnpatchedMods.ObjectEffect().WinningOverrides())
            {
                DisableAssociatedMagicSchools(ench);
            }
        }

        private void DisableAssociatedMagicSchools(IObjectEffectGetter e)
        {
            if (!(e.EnchantType == ObjectEffect.EnchantTypeEnum.StaffEnchantment || e.CastType == CastType.ConstantEffect))
                return;
            foreach (var effect in e.Effects)
            {
                var me = effect.BaseEffect.TryResolve(State.LinkCache);
                if (me == null) continue;
                if (me.MagicSkill == ActorValue.None) continue;
                
                var meo = Patch.MagicEffects.GetOrAddAsOverride(me);
                meo.MagicSkill = ActorValue.None;
            }
        }

        private void DisableAssociatedMagicSchools(ISpellGetter s)
        {
            if (!(s.Type == SpellType.Ability || s.CastType == CastType.ConstantEffect))
                return;
            foreach (var effect in s.Effects)
            {
                var me = effect.BaseEffect.TryResolve(State.LinkCache);
                if (me == null) continue;
                if (me.MagicSkill == ActorValue.None) continue;
                
                var meo = Patch.MagicEffects.GetOrAddAsOverride(me);
                meo.MagicSkill = ActorValue.None;
            }
        }
    }
}