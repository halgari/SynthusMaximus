using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Oblivion;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Global;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Perk;
using static Mutagen.Bethesda.Skyrim.MagicEffect.Flag;
using static Mutagen.Bethesda.Skyrim.MagicEffectArchetype.TypeEnum;
using static SynthusMaximus.Data.Statics;
using CompareOperator = Mutagen.Bethesda.Skyrim.CompareOperator;
using Condition = Mutagen.Bethesda.Skyrim.Condition;
using IMagicEffect = Mutagen.Bethesda.Skyrim.IMagicEffect;
using MagicEffect = Mutagen.Bethesda.Skyrim.MagicEffect;

namespace SynthusMaximus.Patchers
{
    public class MagicEffectPatcher : APatcher<MagicEffectPatcher>
    {
        public MagicEffectPatcher(ILogger<MagicEffectPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        protected override void RunPatcherInner()
        {
            foreach (var me in Mods.MagicEffect().WinningOverrides())
            {
                var meo = Patch.MagicEffects.GetOrAddAsOverride(me);
                MarkDisarm(meo);
                MarkShout(meo);
            }
        }
        
        private void MarkShout(IMagicEffect m)
        {
            if (!m.HasKeyword(MagicShout))
                return;
            
            switch (m.Archetype.Type)
            {
                case ValueModifier:
                case Absorb:
                case DualValueModifier when m.Flags.HasFlag(Detrimental):
                    m.Keywords!.Add(xMASPEShoutHarmful);
                    break;
                case SummonCreature:
                    m.Keywords!.Add(xMASPEShoutSummoning);
                    break;
                default:
                    m.Keywords!.Add(xMASPEShoutNonHarmful);
                    break;
            }

            var script = m.GetOrAddScript(SScriptShoutexp);
            script.Properties.Add(new ScriptObjectProperty()
            {
                Name = SScriptShoutexpProperty0,
                Flags = ScriptProperty.Flag.Edited,
                Object = xMATHIShoutExpBase
            });
            script.Properties.Add(new ScriptObjectProperty
            {
                Name = SScriptShoutexpProperty1,
                Flags = ScriptProperty.Flag.Edited,
                Object = PlayerRef
            });
            script.Properties.Add(new ScriptFloatProperty()
            {
                Name = SScriptShoutexpProperty2,
                Flags = ScriptProperty.Flag.Edited,
                Data = GetShoutExpFactor(m)
            });
            
        }

        private float GetShoutExpFactor(IMagicEffect magicEffect)
        {
            return 1.0f;
        }

        private void MarkDisarm(MagicEffect m)
        {
            if (m.Archetype.Type != Disarm) return;
            
            m.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            m.Keywords.Add(xMAWeapSchoolLightWeaponry);

            m.Conditions.Add(new ConditionFloat
            {
                Data = new FunctionConditionData
                {
                    Function = Condition.Function.WornHasKeyword,
                    Reference = xMAWeapSchoolLightWeaponry,
                    RunOnType = Condition.RunOnType.Subject
                },
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 0.0f,
                Flags = Condition.Flag.OR
            });
                
            m.Conditions.Add(new ConditionFloat
            {
                Data = new FunctionConditionData
                {
                    Function = Condition.Function.HasPerk,
                    Reference = xMALIASecureGrip,
                    RunOnType = Condition.RunOnType.Subject
                },
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 0.0f,
                Flags = Condition.Flag.OR
            });
        }
    }
}