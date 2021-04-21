using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;
using SynthusMaximus.Support;
using SynthusMaximus.Support.RunSorting;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Weapon;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.EquipType;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Dragonborn.Keyword;

namespace SynthusMaximus.Patchers
{
    [RunAfter(typeof(WeaponPatcher))]
    [RunAfter(typeof(ArmorPatcher))]
    public class CraftablePatcher : APatcher<CraftablePatcher>
    {
        private Eager<List<(IConstructibleObjectGetter Cobj, IConstructibleGetter? Constructable)>> _armorWeaponRecipes;

        public CraftablePatcher(ILogger<CraftablePatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
            _armorWeaponRecipes = new Eager<List<(IConstructibleObjectGetter Cobj, IConstructibleGetter? Constructable)>>(() =>
            {
                return UnpatchedMods.ConstructibleObject().WinningOverrides()
                    .AsParallel()
                    .Select(p => (p, p.CreatedObject.TryResolve(State.LinkCache)))
                    .Where(p => p.Item2 != null && (p.Item2 is IWeaponGetter || p.Item2 is IArmorGetter))
                    .ToList();
            });
        }

        protected override void RunPatcherInner()
        {
            foreach (var (c, resolved) in _armorWeaponRecipes.Value)
            {
                try
                {
                    if (Storage.UseMage && c.WorkbenchKeyword.FormKey == DLC2StaffEnchanter.FormKey
                                        && resolved is IWeaponGetter wg
                                        && Storage.StaffCraftingDisableCraftingExclusions.Matches(wg))
                    {
                        DisableRecipe(c);
                    }

                    if (Storage.UseWarrior)
                    {
                        if (c.WorkbenchKeyword.FormKey == CraftingSmithingSharpeningWheel.FormKey
                            && (resolved is IWeaponGetter w))
                        {
                            AlterTemperingRecipe(c, w);
                        }
                        else if (c.WorkbenchKeyword.FormKey == CraftingSmithingSharpeningWheel.FormKey
                                 && resolved is IArmorGetter a)
                        {
                            AlterTemperingRecipe(c, a);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Failed(ex, resolved!);
                }

                Success(resolved!);
            }
            
            
        }

        private void AlterTemperingRecipe(IConstructibleObjectGetter c, IWeaponGetter w)
        {
            var wm = Storage.GetWeaponMaterial(w);
            if (wm == default)
            {
                Ignore(w, "No weapon material");
                return;
            }

            var perk = wm.Type.Data?.SmithingPerk;
            if (perk != null)
            {
                var co = Patch.ConstructibleObjects.GetOrAddAsOverride(c);
                co.Conditions.Clear();
                co.AddCraftingPerkCondition(perk);
            }
        }
        
        private void AlterTemperingRecipe(IConstructibleObjectGetter c, IArmorGetter a)
        {
            var am = Storage.GetArmorMaterial(a);
            if (am == default)
                return;

            var perk = am.Type.Data?.SmithingPerk;
            if (perk != null)
            {
                var co = Patch.ConstructibleObjects.GetOrAddAsOverride(c);
                co.Conditions.Clear();
                co.AddCraftingPerkCondition(perk);
            }
        }

        private void DisableRecipe(IConstructibleObjectGetter c)
        {
            var co = Patch.ConstructibleObjects.GetOrAddAsOverride(c);
            co.WorkbenchKeyword.SetTo(ActorTypeNPC);
            
        }
    }
}