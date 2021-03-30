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
    public class AlchemyPatcher : IPatcher
    {
        private ILogger<AlchemyPatcher> _logger;
        private DataStorage _storage;
        private IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
        private ISkyrimMod _patch;
        private IEnumerable<IModListing<ISkyrimModGetter>> _mods;
        private Dictionary<FormKey, IMagicEffectGetter> _magicEffects;

        public AlchemyPatcher(ILogger<AlchemyPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            _logger = logger;
            _storage = storage;
            _state = state;
            _patch = _state.PatchMod;
            _mods = _state.LoadOrder.PriorityOrder;
            _logger.LogInformation("ArmorPatcher initialized");
        }
        public void RunPatcher()
        {
            _magicEffects = _mods.MagicEffect().WinningOverrides().ToDictionary(e => e.FormKey);
            foreach (var al in _mods.Ingestible().WinningOverrides())
            {
                try
                {
                    if (!_storage.UseThief) continue;
                    if (_storage.IsAlchemyExcluded(al)) continue;
                    MakePotionWorkOverTime(al);
                    DisableAssociatedMagicSchool(al.Effects);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "While processing {EditorID}", al.EditorID);
                }
            }

            foreach (var i in _mods.Ingredient().WinningOverrides())
            {
                try
                {
                    if (!_storage.UseThief) continue;
                    if (_storage.IsIngredientExcluded(i)) continue;
                    MakeIngredientWorkOverTime(i);
                    DisableAssociatedMagicSchool(i.Effects);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "While processing {EditorID}", i.EditorID);
                }
            }
            
        }

        private void MakeIngredientWorkOverTime(IIngredientGetter ig)
        {
            var i = _patch.Ingredients.GetOrAddAsOverride(ig);
            foreach (var effect in i.Effects)
            {
                var oldDur = effect.Data!.Duration;
                
                
                var m = _patch.MagicEffects.GetOrAddAsOverride(_magicEffects[effect.BaseEffect.FormKey]);

                var ae = _storage.GetAlchemyEffect(m);
                if (ae == null)
                {
                    _logger.LogWarning("No effect for {EditorID}", m.EditorID);
                    continue;
                }
                var pm = _storage.GetIngredientVariation(ig);
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
                var m = _patch.MagicEffects.GetOrAddAsOverride(_magicEffects[effect.BaseEffect.FormKey]);
                m.MagicSkill = ActorValue.None;
            }
        }

        private void MakePotionWorkOverTime(IIngestibleGetter alg)
        {
            var al = _patch.Ingestibles.GetOrAddAsOverride(alg);
            foreach (var effect in al.Effects)
            {
                var oldDur = effect.Data!.Duration;
                
                
                var m = _patch.MagicEffects.GetOrAddAsOverride(_magicEffects[effect.BaseEffect.FormKey]);

                var ae = _storage.GetAlchemyEffect(m);
                if (ae == null)
                {
                    _logger.LogWarning("No effect for {EditorID}", m.EditorID);
                    continue;
                }
                var pm = _storage.GetPotionMultipiler(al);
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
                        var mo = _patch.MagicEffects.GetOrAddAsOverride(m);
                        mo.Flags.SetFlag(MagicEffect.Flag.NoDuration, false);
                        mo.Description = m.Description + " [" + _storage.GetOutputString(SDuration)
                                         + ": " + SDurReplace + " "
                                         + _storage.GetOutputString(SSeconds) + "]";
                    }

                    effect.Data.Duration = (int)newDuration;

                }

                effect.Data.Magnitude = newMagnitude;
                m.BaseCost = newCost;
            }
        }
    }
}