using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using static SynthusMaximus.Data.Statics;

namespace SynthusMaximus.Patchers
{
    public class AlchemyPatcher : APatcher<AlchemyPatcher>
    {
        private Dictionary<FormKey, IMagicEffectGetter> _magicEffects;
        
        public AlchemyPatcher(ILogger<AlchemyPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : 
            base(logger, storage, state)
        {
        }

        protected override void RunPatcherInner()
        {
            _magicEffects = Mods.MagicEffect().WinningOverrides().ToDictionary(e => e.FormKey);
            foreach (var al in Mods.Ingestible().WinningOverrides())
            {
                try
                {
                    if (!Storage.UseThief) continue;
                    if (Storage.PotionExclusions.Matches(al)) continue;
                    MakePotionWorkOverTime(al);
                    DisableAssociatedMagicSchool(al.Effects);
                }
                catch (Exception ex)
                {
                    Failed(ex, al);
                }
            }

            foreach (var i in Mods.Ingredient().WinningOverrides())
            {
                try
                {
                    if (!Storage.UseThief) continue;
                    if (Storage.IngredientExclusions.Matches(i)) continue;
                    MakeIngredientWorkOverTime(i);
                    DisableAssociatedMagicSchool(i.Effects);
                }
                catch (Exception ex)
                {
                    Failed(ex, i);
                }
            }
            
        }

        private void MakeIngredientWorkOverTime(IIngredientGetter ig)
        {
            var i = Patch.Ingredients.GetOrAddAsOverride(ig);
            foreach (var effect in i.Effects)
            {
                var oldDur = effect.Data!.Duration;
                
                
                var m = Patch.MagicEffects.GetOrAddAsOverride(_magicEffects[effect.BaseEffect.FormKey]);

                var ae = Storage.GetAlchemyEffect(m);
                if (ae == null)
                {
                    Ignore(m, "No Effect");
                    continue;
                }
                var pm = Storage.GetIngredientVariation(ig);
                var newDuration = ae.BaseDuration;
                var newMagnitude = ae.BaseMagnitude;
                var newCost = ae.BaseCost;

                if (pm != null && ae.AllowPotionMultiplier)
                {
                    newDuration *= pm.MultiplierDuration;
                    newMagnitude *= pm.MultiplierMagnitude;
                }
                else
                {
                    continue;
                }

                effect.Data.Duration = (int)newDuration;
                effect.Data.Magnitude = newMagnitude;
                m.BaseCost = newCost;
            }
        }

        private void DisableAssociatedMagicSchool(IEnumerable<IEffectGetter> effects)
        {
            foreach (var effect in effects)
            {
                var m = Patch.MagicEffects.GetOrAddAsOverride(_magicEffects[effect.BaseEffect.FormKey]);
                m.MagicSkill = ActorValue.None;
            }
        }

        private void MakePotionWorkOverTime(IIngestibleGetter alg)
        {
            var al = Patch.Ingestibles.GetOrAddAsOverride(alg);
            foreach (var effect in al.Effects)
            {
                var oldDur = effect.Data!.Duration;
                
                
                var m = Patch.MagicEffects.GetOrAddAsOverride(_magicEffects[effect.BaseEffect.FormKey]);

                var ae = Storage.GetAlchemyEffect(m);
                if (ae == null)
                {
                    Ignore(m, "No Effect");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(al.NameOrEmpty()))
                {
                    Ignore(al, "Empty Name");
                    continue;
                }
                
                var pm = Storage.GetPotionMultipiler(al);
                var newDuration = ae.BaseDuration;
                var newMagnitude = ae.BaseMagnitude;
                var newCost = ae.BaseCost;

                if (pm != null && ae.AllowPotionMultiplier)
                {
                    newDuration *= pm.MultiplierDuration;
                    newMagnitude *= pm.MultiplierMagnitude;
                }
                else
                {
                    continue;
                }

                if (oldDur != newDuration && newDuration >= 0)
                {
                    if (!m.Description.NameOrEmpty().Contains(SDurReplace))
                    {
                        var mo = Patch.MagicEffects.GetOrAddAsOverride(m);
                        mo.Flags.SetFlag(MagicEffect.Flag.NoDuration, false);
                        mo.Description = m.Description + " [" + Storage.GetOutputString(SDuration)
                                         + ": " + SDurReplace + " "
                                         + Storage.GetOutputString(SSeconds) + "]";
                    }

                    effect.Data.Duration = (int)newDuration;

                }

                effect.Data.Magnitude = newMagnitude;
                m.BaseCost = newCost;
            }
        }


    }
}