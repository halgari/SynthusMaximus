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
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.ObjectEffect;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Perk;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Noggog;
using SynthusMaximus.Data.DTOs;
using SynthusMaximus.Data.DTOs.Weapon;
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
            var weapons = _state.LoadOrder.PriorityOrder.Weapon().WinningOverrides().ToArray();
            _logger.LogInformation("Patching {Count} weapons", weapons.Length);
            foreach (var w in weapons)
            {
                try
                {
                    if (_storage.UseWarrior)
                    {
                        if (string.IsNullOrEmpty(w.NameOrEmpty()))
                            continue;
                        
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

                        var wm = _storage.GetWeaponMaterial(w);
                        if (wm?.Type.Data == default)
                        {
                            WeaponsWithoutMaterialOrType.Add(w.FormKey);
                            continue;
                        }

                        var wt = _storage.GetWeaponType(w);
                        if (wt == default)
                        {
                            WeaponsWithoutMaterialOrType.Add(w.FormKey);
                            continue;
                        }

                        var wp = _state.PatchMod.Weapons.GetOrAddAsOverride(w);

                        AddSpecificKeyword(wp, wt);
                        AddGenericKeyword(wp, wt);

                        if (Equals(wt.BaseWeaponType.Data.School, xMAWeapSchoolRangedWeaponry))
                            wp.Data!.Flags |= WeaponData.Flag.NPCsUseAmmo;

                        if (_storage.UseWarrior)
                        {
                            if (_storage.ShouldAppendWeaponType)
                                AppendTypeToName(wp, wt);

                            AddCombatLogicKeywords(wp, wt);
                            ModStats(wp, wt, wm);

                            if (!w.Data!.Flags.HasFlag(WeaponData.Flag.CantDrop))
                            {
                                AddMeltdownRecipe(wp, wt, wm);

                                if (!_storage.IsWeaponExcludedReforged(w))
                                {
                                    CreateRefinedSilverWeapon(wp);

                                }
                            }
                        }

                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in patcher {EditorID}", w.EditorID);
                }
            }
        }

        private void CreateRefinedSilverWeapon(Weapon w)
        {
            if (!w.HasKeyword(WeapMaterialSilver))
                return;

            var newName = _storage.GetOutputString("Refined") + " " + w.NameOrThrow();

            var nw = _state.PatchMod.Weapons.DuplicateInAsNewRecord(w);
            nw.Name = newName;
            nw.Description = SWeaponRefinedDesc;
            nw.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            nw.Keywords.Add(xMAWeapMaterialSilverRefined);
            nw.Keywords.Add(WeapMaterialSilver);

            var wm = _storage.GetWeaponMaterial(nw);
            var wt = _storage.GetWeaponType(nw);
            
            ModStats(nw, wt, wm);
            ApplyModifiers(nw);
            
            // Swap properties on silver sword script
            var script = nw.GetOrAddScript(SScriptSilversword);
            script.Properties.Add(new ScriptObjectProperty()
            {
                Name = SScriptApplyperkProperty,
                Object = xMAWeapMaterialSilverRefined
            });

            if (!_storage.IsWeaponExcludedReforged(w))
            {
                var reforged = CreateReforgedWeapon(nw, wt, wm);
                ApplyModifiers(reforged);
                AddReforgedCraftingRecipe(reforged, w, wm);
                AddMeltdownRecipe(reforged, wt, wm);
                AddTemperingRecipe(reforged, wm);

                var warforged = CreateWarforgedWeapon(nw, wt, wm);
                AddWarforgedCraftingRecipe(warforged, reforged, wm);
                AddMeltdownRecipe(warforged, wt, wm);
                AddTemperingRecipe(warforged, wm);
                ApplyModifiers(warforged);
            }

            DoCopycat(w, wm, wt);
            DistributeWeaponOnLeveledList(w, wm, wt);
        }

        private void DistributeWeaponOnLeveledList(Weapon w, WeaponMaterial wm, WeaponType wt)
        {
            if (w.Data!.Flags.HasFlag(WeaponData.Flag.CantDrop) || w.Data!.Flags.HasFlag(WeaponData.Flag.BoundWeapon))
                return;

            if (_storage.IsWeaponExcludedDistribution(w))
                return;

            var flink = new FormLink<IItemGetter>(w.FormKey);
            bool similarSet = false;
            IWeaponGetter? firstSimilarMatch = default;
            
            foreach (var i in _state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().ToArray())
            {
                IWeaponGetter? wl = default;
                
                if (_storage.IsListExcludedWeaponRegular(i))
                    continue;
                
                if (i.Entries?.Any(e => Equals(e.Data?.Reference, flink)) ?? false)
                    continue;

                foreach (var li in i.Entries ?? new List<ILeveledItemEntryGetter>())
                {
                    // Only consider weapons
                    if (!(li.Data?.Reference.TryResolve<IWeaponGetter>(_state.LinkCache, out wl) ?? false))
                        continue;

                    if (!similarSet)
                    {
                        if (!AreWeaponsSimilar(w, wm, wt, wl))
                        {
                            similarSet = true;
                            firstSimilarMatch = wl;
                        }
                        else
                        {
                            if (!Equals(wl, firstSimilarMatch))
                                continue;
                        }
                    }

                    var nw = _state.PatchMod.LeveledItems.DuplicateInAsNewRecord(i);
                    nw.Entries ??= new ExtendedList<LeveledItemEntry>();
                    nw.Entries!.Add(new LeveledItemEntry
                    {
                        Data = new LeveledItemEntryData
                        {
                            Level = li.Data.Level,
                            Count = li.Data.Count,
                            Reference = new FormLink<IItemGetter>(w.FormKey)
                        }
                    });
                    

                }
                
            }
            

        }

        private bool AreWeaponsSimilar(Weapon w1, WeaponMaterial wm1, WeaponType wt1, IWeaponGetter w2)
        {
            if (WeaponsWithoutMaterialOrType.Contains(w2.FormKey))
                return false;

            var wm2 = _storage.GetWeaponMaterial(w2);
            var wt2 = _storage.GetWeaponType(w2);

            if (wm2?.Type.Data == null || wt2 == null)
                return false;

            return wm1.Type.Data?.TemperingInput == wm2.Type.Data.TemperingInput &&
                   wt1.BaseWeaponType.Equals(wt2.BaseWeaponType) &&
                   DoWeaponsContainClasses(w1, w2) &&
                   Equals(w1.ObjectEffect, w2.ObjectEffect);

        }

        private bool DoWeaponsContainClasses(IWeaponGetter w1, IWeaponGetter w2)
        {
            if (w1.HasKeyword(xMAWeapClassBlade) && !w2.HasKeyword(xMAWeapClassBlade))
                return false;
            if (w1.HasKeyword(xMAWeapClassBlunt) && !w2.HasKeyword(xMAWeapClassBlunt))
                return false;
            if (w1.HasKeyword(xMAWeapClassPiercing) && !w2.HasKeyword(xMAWeapClassPiercing))
                return false;
            return true;
        }

        private void DoCopycat(Weapon w, WeaponMaterial wm, WeaponType wt)
        {
            if (!w.HasAnyKeyword(DaedricArtifact, WeapTypeStaff))
                return;

            var nw = CreateCopycatWeapon(w);
            CreateCopycatRecipe(nw, w, wm);
            
            AddMeltdownRecipe(nw, wt, wm);
            AddTemperingRecipe(nw, wm);
            CreateRefinedSilverWeapon(nw);
            CreateReforgedWeapon(nw, wt, wm);
            ApplyModifiers(nw);
        }

        private void CreateCopycatRecipe(Weapon w, Weapon oldWeapon, WeaponMaterial wm)
        {
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixWeapon + SPrefixCrafting + w.EditorID + w.FormKey;
            cobj.WorkbenchKeyword.SetTo(CraftingSmithingForge);
            cobj.CreatedObject.SetTo(w);

            var input = wm.Type.Data.TemperingInput;
            var materialPerk = wm.Type.Data.SmithingPerk;
            
            if (input != null)
                cobj.AddCraftingRequirement(input, 3);
            
            cobj.AddCraftingPerkCondition(xMASMICopycat);
            cobj.AddCraftingRequirement(xMASMICopycatArtifactEssence, 1);
            
            if (materialPerk != null)
                cobj.AddCraftingPerkCondition(materialPerk);
            
            cobj.AddCraftingInventoryCondition(oldWeapon);
        }

        private Weapon CreateCopycatWeapon(Weapon w)
        {
            var newName = w.NameOrThrow() + "[" + _storage.GetOutputString(SReplica) + "]";
            var nw = _state.PatchMod.Weapons.DuplicateInAsNewRecord(w);
            nw.EditorID = SPrefixPatcher + SPrefixWeapon + w.EditorID + "Replica" + nw.FormKey;
            nw.Name = newName;
            nw.ObjectEffect.SetToNull();
            nw.EnchantmentAmount = 0;
            return nw;
        }

        private void AddWarforgedCraftingRecipe(Weapon w, Weapon? oldWeapon, WeaponMaterial wm)
        {
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixWeapon + SPrefixCrafting + w.EditorID + w.FormKey;
            cobj.WorkbenchKeyword.SetTo(CraftingSmithingForge);
            cobj.CreatedObject.SetTo(w);
            var ing2 = wm.Type.Data.TemperingInput;

            if (ing2 == null)
                ing2 = wm.Type.Data.BreakdownProduct;
            
            if (oldWeapon != null)
                cobj.AddCraftingRequirement(oldWeapon, 1);
            
            cobj.AddCraftingRequirement(ing2!, 5);
            
            cobj.AddCraftingPerkCondition(xMASMIMasteryWarforged);
            
            if (wm.Type.Data.SmithingPerk != null)
                cobj.AddCraftingPerkCondition(wm.Type.Data.SmithingPerk);
            
            cobj.AddCraftingInventoryCondition(oldWeapon);
        }

        private Weapon CreateWarforgedWeapon(Weapon w, WeaponType wt, WeaponMaterial wm)
        {
            var newName = _storage.GetOutputString("Warforged") + " " + w.NameOrEmpty();
            var nw = _state.PatchMod.Weapons.DuplicateInAsNewRecord(w);
            nw.EditorID = SPrefixPatcher + SPrefixWeapon + w.EditorID + w.FormKey;
            nw.Name = newName;
            nw.ObjectEffect.SetTo(xMASMIMasteryWarforgedEnchWeapon);
            nw.EnchantmentAmount = 10;
            nw.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            nw.Keywords.Add(xMASMIWarforgedWeaponKW);
            nw.Keywords.Add(MagicDisallowEnchanting);
            return nw;
        }

        private void AddTemperingRecipe(Weapon w, WeaponMaterial wm)
        {
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixWeapon + SPrefixTemper + w.EditorID + w.FormKey;
            cobj.WorkbenchKeyword.SetTo(CraftingSmithingSharpeningWheel);
            cobj.CreatedObject.SetTo(w);
            cobj.CreatedObjectCount = 1;

            var ing = wm.Type.Data.TemperingInput;
            if (wm.Type.Data.TemperingInput == null)
            {
                _logger.LogWarning("No tempering item found for {EditorID} will use meltdown product", w.EditorID);
                ing = wm.Type.Data.BreakdownProduct;
            }

            if (ing != null)
            {
                cobj.AddCraftingRequirement(ing, 1);
            }
            else
            {
                _logger.LogWarning("No input found for tempering recipe for {EditorID}", w.EditorID);
            }

            var perk = wm.Type.Data.SmithingPerk;
            if (perk != null)
                cobj.AddCraftingPerkCondition(perk);
        }

        private void AddReforgedCraftingRecipe(Weapon newWeapon, Weapon? oldWeapon, WeaponMaterial wm)
        {
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixWeapon + SPrefixCrafting + newWeapon.EditorID + newWeapon.FormKey;
            cobj.WorkbenchKeyword.SetTo(CraftingSmithingForge);

            var ing1 = oldWeapon;
            var ing2 = wm.Type.Data.TemperingInput;

            if (ing2 == null)
                ing2 = wm.Type.Data.BreakdownProduct;
            
            if (ing1 != null)
                cobj.AddCraftingRequirement(ing1, 1);
            
            if (ing2 != null)
                cobj.AddCraftingRequirement(ing2, 2);
            
            cobj.AddCraftingPerkCondition(xMASMIWeaponsmith);
            
            if (wm.Type.Data.SmithingPerk != null)
                cobj.AddCraftingPerkCondition(wm.Type.Data.SmithingPerk);
            
            cobj.AddCraftingInventoryCondition(ing1);
        }

        private Weapon CreateReforgedWeapon(Weapon w, WeaponType wt, WeaponMaterial wm)
        {
            var newName = _storage.GetOutputString("Reforged") + " " + w.NameOrEmpty();
            var nw = _state.PatchMod.Weapons.DuplicateInAsNewRecord(w);
            nw.Name = newName;
            return nw;
        }

        private void ApplyModifiers(Weapon w)
        {
            var modifiers = _storage.GetAllModifiers(w);
            foreach (var m in modifiers)
            {
                w.BasicStats!.Damage = (ushort)(w.BasicStats.Damage * m.FactorDamage);
                w.Critical!.Damage = (ushort) (w.Critical!.Damage * m.FactorCritDamage);
                w.Data!.Speed *= m.FactorAttackSpeed;
                w.BasicStats.Weight = (ushort) (w.BasicStats.Weight * m.FactorWeight);
                w.Data!.Reach = (ushort) (w.Data!.Reach * m.FactorReach);
                w.BasicStats.Value = (uint) (w.BasicStats.Value * m.FactorValue);
            }
        }

        private void AddMeltdownRecipe(Weapon w, WeaponType wt, WeaponMaterial wm)
        {
            var requiredPerk = wm.Type.Data!.SmithingPerk;
            var inputNum = wt.MeltdownInput;
            var outputNum = wt.MeltdownOutput;

            if (wm.Type.Data!.BreakdownProduct == default || inputNum <= 0 || outputNum <= 0)
                return;

            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixWeapon + SPrefixMeltdown + w.EditorID + w.FormKey.ToString();
            cobj.AddCraftingRequirement(w, wt.MeltdownInput);
            cobj.CreatedObject.SetTo(wm.Type.Data.BreakdownProduct!);
            cobj.CreatedObjectCount = outputNum;
            
            if (requiredPerk != default)
                cobj.AddCraftingPerkCondition(requiredPerk);
            
            cobj.AddCraftingInventoryCondition(w);
            cobj.AddCraftingPerkCondition(xMASMIMeltdown);
        }

        private void AddCombatLogicKeywords(Weapon w, WeaponType wt)
        {
            var bleedKw = wt.BleedTier.GetDefinition().BleedKeyword;
            var staggerKw = wt.StaggerTier.GetDefinition().StaggerKeyword;
            var debuffKw = wt.DebuffTier.GetDefinition().DebuffKeyword;

            w.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            
            if (bleedKw != default)
                w.Keywords.Add(bleedKw);
            
            if (staggerKw != default)
                w.Keywords.Add(staggerKw);
            
            if (debuffKw != default)
                w.Keywords.Add(debuffKw);
        }

        private void ModStats(Weapon w, WeaponType wt, WeaponMaterial wm)
        {
            SetReach(w, wt, wm);
            SetDamage(w, wt, wm);
            SetCritDamage(w, wt, wm);
            SetSpeed(w, wt, wm);
        }

        private void SetSpeed(Weapon w, WeaponType wt, WeaponMaterial wm)
        {
            w.Data!.Speed = wt.SpeedBase + wm.SpeedModifier;
        }

        private void SetCritDamage(Weapon w, WeaponType wt, WeaponMaterial wm)
        {
            w.Critical!.Damage = (ushort)(w.BasicStats!.Damage * wt.CritDamageFactor);
        }

        private void SetDamage(Weapon w, WeaponType wt, WeaponMaterial wm)
        {
            var skillBase = _storage.GetWeaponSkillDamageBase(wt.BaseWeaponType);
            var typeMod = wt.DamageBase;
            var matMod = wm.DamageModifier;
            var typeMult = _storage.GetWeaponSkillDamageMultipler(wt.BaseWeaponType);

            if (skillBase == null || typeMult == null)
                return;

            w.BasicStats!.Damage = (ushort) (skillBase + typeMod + matMod);
        }

        private void SetReach(Weapon w, WeaponType wt, WeaponMaterial wm)
        {
            w.Data!.Reach = wt.ReachBase + wm.ReachModifier;
        }

        private void AppendTypeToName(Weapon w, WeaponType wt)
        {
            var id = wt.Name;
            var name = w.NameOrThrow();

            if (name.Contains(id, StringComparison.InvariantCultureIgnoreCase))
                return;

            w.Name = $"{name} [{id.ToLower()}]";
        }

        private void AddGenericKeyword(IWeapon w, WeaponType wt)
        {
            w.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            
            if (w.Data!.Flags.HasFlag(WeaponData.Flag.BoundWeapon))
                w.Keywords.Add(xMACONBoundWeaponKW);
            
            w.Keywords.AddRange(wt.WeaponClass.Data.Keywords);
        }

        private void AddSpecificKeyword(IWeapon w, WeaponType wt)
        {
            w.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            var baseType = wt.BaseWeaponType;
            w.Keywords.Add(baseType.Data.Keyword);
            w.Keywords.Add(baseType.Data.School);
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
            AlterTemperingRecipe(w, wo.MaterialTempering.Data);
            AddMeltdownRecipe(w, wo.MaterialMeltdown.Data, wo.MeltdownInput, wo.MeltdownOutput);
        }

        private void AddMeltdownRecipe(Weapon w, Material bmw, ushort meltdownIn, ushort meltdownOut)
        {
            var perk = bmw.SmithingPerk;
            var output = bmw.BreakdownProduct;
            var benchKW = bmw.BreakdownStation;

            if (output == null || meltdownIn <= 0 || meltdownOut <= 0)
            {
                _logger.LogInformation("Weapon {EditorID}: no meltdown recipe generated", w.EditorID);
                return;
            }

            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixWeapon + SPrefixMeltdown + w.EditorID + w.FormKey;
            
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(w), meltdownIn);
            cobj.CreatedObject.SetTo(bmw.BreakdownProduct!);
            cobj.CreatedObjectCount = meltdownOut;
            cobj.WorkbenchKeyword.SetTo(benchKW);
            
            if (perk != null)
                cobj.AddCraftingPerkCondition(perk);
            
            cobj.AddCraftingInventoryCondition(new FormLink<ISkyrimMajorRecordGetter>(w), meltdownIn);
            cobj.AddCraftingPerkCondition(xMASMIMeltdown);

        }

        private void AlterTemperingRecipe(Weapon w, Material bmw)
        {
            foreach (var c in _state.LoadOrder.PriorityOrder.ConstructibleObject().WinningOverrides()
                .Where(c => c.CreatedObject.FormKey == w.FormKey &&
                            c.WorkbenchKeyword.FormKey == CraftingSmithingSharpeningWheel.FormKey)
                .Select(c => _state.PatchMod.ConstructibleObjects.GetOrAddAsOverride(c)))
            {
                c.Conditions.Clear();
                var perk = bmw.SmithingPerk;
                if (perk != null)
                {
                    c.AddCraftingPerkCondition(perk);
                }
            }
        }
    }
}