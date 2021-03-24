using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;
using SynthusMaximus.Data.LowLevel;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static SynthusMaximus.Data.Statics;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Perk;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Perk;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Patchers
{
    public class WeaponPatcher : IPatcher
    {
        private ILogger<WeaponPatcher> _logger;
        private DataStorage _storage;
        private IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;

        public WeaponPatcher(ILogger<WeaponPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            _logger = logger;
            _storage = storage;
            _state = state;
        }
        public void RunPatcher()
        {
            foreach (var w in _state.LoadOrder.PriorityOrder.Weapon().WinningOverrides())
            {
                try
                {
                    if (_storage.UseWarrior)
                    {
                        var wo = _storage.GetWeaponOverride(w);

                        if (wo != null)
                        {
                            ApplyWeaponOverride(w, wo);
                        }

                        if (!ShouldPatch(w))
                        {
                            _logger.LogTrace("{EditorID} : Ignored", w.EditorID);
                            continue;
                        }

                        var wt = _storage.GetWeaponType(w);

                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in patcher {EditorID}", w.EditorID);
                }
            }
        }

        private bool ShouldPatch(IWeaponGetter w)
        {
            if (!w.Template.IsNull)
                return false;
            if (w.HasKeyword(WeapTypeStaff))
                return false;
            if (w.Name == null || !w.Name!.TryLookup(Language.English, out var name) || string.IsNullOrEmpty(name))
                return false;
            if (WeaponsWithoutMaterialOrType.Contains(w.FormKey))
                return false;
            return true;

        }

        public HashSet<FormKey> WeaponsWithoutMaterialOrType { get; set; } = new();

        private void ApplyWeaponOverride(IWeaponGetter wg, WeaponOverride wo)
        {
            var w = _state.PatchMod.Weapons.GetOrAddAsOverride(wg);
            w.BasicStats!.Damage = wo.Damage;
            w.Data!.Reach = wo.Reach;
            w.Data!.Speed = wo.Speed;
            w.Critical!.Damage = wo.CritDamage;
            w.Name = w.NameOrThrow() + wo.StringToAppend;
            AlterTemperingRecipe(w, wo.MaterialTempering);
            AddMeltdownRecipe(w, wo.MaterialMeltdown, wo.MeltdownInput, wo.MeltdownOutput);
        }

        private void AddMeltdownRecipe(Weapon w, BaseMaterialWeapon bmw, ushort meltdownIn, ushort meltdownOut)
        {
            var definition = bmw.GetDefinition();
            var perk = definition.SmithingPerk;
            var output = definition.MeltdownProduct;
            var benchKW = definition.MeltdownCraftingStation;

            if (output == null || meltdownIn <= 0 || meltdownOut <= 0)
            {
                _logger.LogInformation("Weapon {EditorID}: no meltdown recipe generated", w.EditorID);
                return;
            }

            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixWeapon + SPrefixMeltdown + w.EditorID + w.FormKey;
            
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(w), meltdownIn);
            cobj.CreatedObject.SetTo(definition.MeltdownProduct!);
            cobj.CreatedObjectCount = meltdownOut;
            cobj.WorkbenchKeyword.SetTo(benchKW);
            
            if (perk != null)
                cobj.AddCraftingPerkCondition(perk);
            
            cobj.AddCraftingInventoryCondition(new FormLink<ISkyrimMajorRecordGetter>(w), meltdownIn);
            cobj.AddCraftingPerkCondition(xMASMIMeltdown);

        }

        private void AlterTemperingRecipe(Weapon w, BaseMaterialWeapon bmw)
        {
            foreach (var c in _state.LoadOrder.PriorityOrder.ConstructibleObject().WinningOverrides()
                .Where(c => c.CreatedObject.FormKey == w.FormKey &&
                            c.WorkbenchKeyword.FormKey == CraftingSmithingSharpeningWheel.FormKey)
                .Select(c => _state.PatchMod.ConstructibleObjects.GetOrAddAsOverride(c)))
            {
                c.Conditions.Clear();
                var perk = bmw.GetDefinition().SmithingPerk;
                if (perk != null)
                {
                    c.AddCraftingPerkCondition(perk);
                }
            }
        }
    }
}