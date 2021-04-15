using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using SynthusMaximus.Support;

namespace SynthusMaximus.Patchers
{
    public class WeaponPatcher : APatcher<WeaponPatcher>
    {
        private Dictionary<FormKey, IWeaponGetter> _weapons;

        private List<(Weapon w, WeaponMaterial wm, WeaponType wt)> _markedForDistribution = new();
        private long _weaponsDistributed = 0;
        private IEnumerable<ILeveledItemGetter> _leveledLists;
        private Eager<Dictionary<(WeaponMaterial?, WeaponType?, FormKey), IEnumerable<IndexedEntry<IWeaponGetter>>>> _indexedLevelLists;

        public WeaponPatcher(ILogger<WeaponPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
            _indexedLevelLists = new Eager<Dictionary<(WeaponMaterial?, WeaponType?, FormKey), IEnumerable<IndexedEntry<IWeaponGetter>>>>
            (() => IndexLeveledLists<IWeaponGetter, (WeaponMaterial?, WeaponType?, FormKey)>
                (f => (Storage.GetWeaponMaterial(f), Storage.GetWeaponType(f), f.ObjectEffect.FormKey)));
        }
        
        public override void RunPatcher()
        {
            
            foreach (var w in Mods.Weapon().WinningOverrides())
            {
                try
                {
                    if (Storage.UseWarrior)
                    {
                        if (string.IsNullOrEmpty(w.NameOrEmpty()))
                            continue;
                        
                        var wo = Storage.GetWeaponOverride(w);

                        if (wo != null)
                        {
                            ApplyWeaponOverride(w, wo);
                        }

                        if (!ShouldPatch(w))
                        {
                            Logger.LogTrace("{EditorID} : Ignored", w.EditorID);
                            continue;
                        }

                        var wm = Storage.GetWeaponMaterial(w);
                        if (wm?.Type.Data == default)
                        {
                            WeaponsWithoutMaterialOrType.Add(w.FormKey);
                            continue;
                        }

                        var wt = Storage.GetWeaponType(w);
                        if (wt == default)
                        {
                            WeaponsWithoutMaterialOrType.Add(w.FormKey);
                            continue;
                        }

                        var wp = Patch.Weapons.GetOrAddAsOverride(w);

                        AddSpecificKeyword(wp, wt);
                        AddGenericKeyword(wp, wt);

                        if (Equals(wt.BaseWeaponType.Data.School, xMAWeapSchoolRangedWeaponry))
                            wp.Data!.Flags |= WeaponData.Flag.NPCsUseAmmo;

                        if (Storage.UseWarrior)
                        {
                            if (Storage.ShouldAppendWeaponType)
                                AppendTypeToName(wp, wt);

                            AddCombatLogicKeywords(wp, wt);
                            ModStats(wp, wt, wm);

                            if (!w.Data!.Flags.HasFlag(WeaponData.Flag.CantDrop))
                            {
                                AddMeltdownRecipe(wp, wt, wm);

                                if (!Storage.WeaponReforgeExclusions.IsExcluded(w))
                                {
                                    CreateRefinedSilverWeapon(wp);

                                    var reforged = CreateReforgedWeapon(wp, wt, wm);
                                    ApplyModifiers(reforged);
                                    AddReforgedCraftingRecipe(reforged, wp, wm);
                                    AddMeltdownRecipe(reforged, wt, wm);
                                    AddTemperingRecipe(reforged, wm);

                                    var warforgedWeapon = CreateWarforgedWeapon(wp, wt, wm);
                                    AddWarforgedCraftingRecipe(warforgedWeapon, reforged, wm);
                                    AddMeltdownRecipe(warforgedWeapon, wt, wm);
                                    AddTemperingRecipe(warforgedWeapon, wm);
                                    ApplyModifiers(warforgedWeapon);

                                    CreateCrossbowVariants(wp, wm, wt);
                                }
                                DoCopycat(wp, wm, wt);
                                MarkForDistribution(wp, wm, wt);
                            }
                        }

                        if (Storage.UseThief)
                        {
                            if (Equals(wt.BaseWeaponType.Data.Keyword, xMAWeapTypeFist))
                            {
                                MarkForDistribution(wp, wm, wt);
                            }
                        }

                    }

                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error in patcher {EditorID}", w.EditorID);
                }
            }

            DistributeMarkedWeapons();
        }

        private void DistributeMarkedWeapons()
        {
            var sw = Stopwatch.StartNew();
            _weapons = Mods.Weapon().WinningOverrides().ToDictionary(f => f.FormKey);
            Logger.LogInformation("About to distribute {Count} items into leveled lists", _weapons.Count);
            _leveledLists = Mods.LeveledItem().WinningOverrides()
                .Where(li => !Storage.DistributionExclusionsWeaponListRegular.IsExcluded(li))
                .ToList();


            
            Logger.LogInformation("Ran distribution prep in {MS}", sw.ElapsedMilliseconds);
            foreach (var t in _markedForDistribution)
            {
                DistributeWeaponOnLeveledList(_indexedLevelLists.Value, t.w, t.wm, t.wt);
            }

            Logger.LogInformation("Finished Distribution in {MS}", sw.ElapsedMilliseconds);

            Logger.LogInformation("Distributed {Count} items", _weaponsDistributed);
        }

        private void CreateCrossbowVariants(IWeaponGetter w, WeaponMaterial wm, WeaponType wt)
        {
            if (!w.HasKeyword(xMAWeapTypeCrossbow))
                return;



            // Make basic crossbows
            var newRecurveCrossbow = ApplyRecurveCrossbowModifications(w);
            CreateEnhancedCrossbowCraftingRecipe(w, newRecurveCrossbow!, wm, 1, xMARANAspiringEngineer0);

            var newLightweightCrossbow = ApplyLightweightCrossbowModifications(w);
            CreateEnhancedCrossbowCraftingRecipe(w, newLightweightCrossbow!, wm, 1, xMARANAspiringEngineer1);
            
            var newArbalestCrossbow = ApplyArbalestCrossbowModifications(w);
            CreateEnhancedCrossbowCraftingRecipe(w, newArbalestCrossbow!, wm, 1, xMARANProficientEngineer0);

            var newSilencedCrossbow = ApplySilencedCrossbowModifications(w);
            CreateEnhancedCrossbowCraftingRecipe(w, newSilencedCrossbow!, wm, 1, xMARANProficientEngineer1);
            
            // Cross-mix them into combinations

            var newRecurveArbalestCrossbow = ApplyArbalestCrossbowModifications(newRecurveCrossbow);
            CreateEnhancedCrossbowCraftingRecipe(newRecurveCrossbow!, newRecurveArbalestCrossbow, wm, 2,
                xMARANProficientEngineer0, xMARANAspiringEngineer0, xMARANCrossbowTechnician);
            CreateEnhancedCrossbowCraftingRecipe(newArbalestCrossbow!, newRecurveArbalestCrossbow, wm, 2,
                xMARANProficientEngineer0, xMARANAspiringEngineer0, xMARANCrossbowTechnician);
            
            var newRecurveSilencedCrossbow = ApplySilencedCrossbowModifications(newRecurveCrossbow);
            CreateEnhancedCrossbowCraftingRecipe(newRecurveCrossbow!, newRecurveSilencedCrossbow, wm, 2,
                xMARANProficientEngineer1, xMARANAspiringEngineer0, xMARANCrossbowTechnician);
            CreateEnhancedCrossbowCraftingRecipe(newSilencedCrossbow!, newRecurveSilencedCrossbow, wm, 2,
                xMARANProficientEngineer1, xMARANAspiringEngineer0, xMARANCrossbowTechnician);
            
            var newRecurveLightweightCrossbow = ApplyLightweightCrossbowModifications(newRecurveCrossbow);
            CreateEnhancedCrossbowCraftingRecipe(newRecurveCrossbow!, newRecurveLightweightCrossbow, wm, 2,
                xMARANAspiringEngineer1, xMARANAspiringEngineer0, xMARANCrossbowTechnician);
            CreateEnhancedCrossbowCraftingRecipe(newLightweightCrossbow!, newRecurveSilencedCrossbow, wm, 2,
                xMARANAspiringEngineer1, xMARANAspiringEngineer0, xMARANCrossbowTechnician);
            
            var newSilencedLightweightCrossbow = ApplyArbalestCrossbowModifications(newSilencedCrossbow);
            CreateEnhancedCrossbowCraftingRecipe(newSilencedCrossbow!, newSilencedLightweightCrossbow, wm, 2,
                xMARANProficientEngineer1, xMARANAspiringEngineer1, xMARANCrossbowTechnician);
            CreateEnhancedCrossbowCraftingRecipe(newLightweightCrossbow!, newSilencedLightweightCrossbow, wm, 2,
                xMARANProficientEngineer1, xMARANAspiringEngineer1, xMARANCrossbowTechnician);
            
            var newSilencedArbalestCrossbow = ApplySilencedCrossbowModifications(newRecurveCrossbow);
            CreateEnhancedCrossbowCraftingRecipe(newSilencedCrossbow!, newSilencedArbalestCrossbow, wm, 2,
                xMARANProficientEngineer0, xMARANProficientEngineer1, xMARANCrossbowTechnician);
            CreateEnhancedCrossbowCraftingRecipe(newArbalestCrossbow!, newSilencedArbalestCrossbow, wm, 2,
                xMARANProficientEngineer0, xMARANProficientEngineer1, xMARANCrossbowTechnician);
            
            var newLightweightArbalest = ApplyLightweightCrossbowModifications(newRecurveCrossbow);
            CreateEnhancedCrossbowCraftingRecipe(newLightweightCrossbow!, newLightweightArbalest, wm, 2,
                xMARANProficientEngineer0, xMARANAspiringEngineer1, xMARANCrossbowTechnician);
            CreateEnhancedCrossbowCraftingRecipe(newArbalestCrossbow!, newLightweightArbalest, wm, 2,
                xMARANProficientEngineer0, xMARANAspiringEngineer1, xMARANCrossbowTechnician);

            List<Weapon> newCrossbows = new()
            {
                newRecurveCrossbow,
                newLightweightCrossbow,
                newArbalestCrossbow,
                newSilencedCrossbow,
                newLightweightArbalest,
                newSilencedArbalestCrossbow,
                newSilencedLightweightCrossbow,
                newRecurveLightweightCrossbow,
                newRecurveSilencedCrossbow,
                newRecurveArbalestCrossbow,
            };

            foreach (var c in newCrossbows)
            {
                AddMeltdownRecipe(c, wt, wm);
                AddTemperingRecipe(c, wm);

                var reforgedWeapon = CreateReforgedWeapon(c, wt, wm);
                ApplyModifiers(reforgedWeapon);
                AddReforgedCraftingRecipe(reforgedWeapon, c, wm);
                AddMeltdownRecipe(reforgedWeapon, wt, wm);
                AddTemperingRecipe(reforgedWeapon, wm);

                var warforgedWeapon = CreateWarforgedWeapon(c, wt, wm);
                AddWarforgedCraftingRecipe(warforgedWeapon, reforgedWeapon, wm);
                AddMeltdownRecipe(warforgedWeapon, wt, wm);
                AddTemperingRecipe(warforgedWeapon, wm);
                ApplyModifiers(warforgedWeapon);
                
                ApplyModifiers(c);
            }
        }

        private Weapon ApplySilencedCrossbowModifications(IWeaponGetter w)
        {
            return ApplyCrossbowModifications(w, xMAWARCrossbowSilenced, SCrossbowSilenced, w =>
            {
                w.DetectionSoundLevel = SoundLevel.Silent;
            });
        }

        private Weapon ApplyArbalestCrossbowModifications(IWeaponGetter w)
        {
            return ApplyCrossbowModifications(w, xMAWARCrossbowArbalest, SCrossbowArbalest, w =>
            {
                w.BasicStats!.Weight *= 1.2f;
            });
        }

        private Weapon ApplyLightweightCrossbowModifications(IWeaponGetter w)
        {
            return ApplyCrossbowModifications(w, xMAWARCrossbowLightweight, SCrossbowLightweight);
        }

        private Weapon ApplyRecurveCrossbowModifications(IWeaponGetter w)
        {
            return ApplyCrossbowModifications(w, xMAWARCrossbowRecurve, SCrossbowRecurve);
        }

        private Weapon? ApplyCrossbowModifications(IWeaponGetter w, IFormLink<IKeywordGetter> kw, string nameToAdd,
            Action<Weapon>? modfn = null)
        {
            if (w.HasKeyword(kw))
                return null;

            var newName = $"{w.NameOrThrow()} [{Storage.GetOutputString(nameToAdd)}]";
            var newCrossbow = Patch.Weapons.DuplicateInAsNewRecord(w);
            newCrossbow.EditorID = SPrefixPatcher + SPrefixWeapon + newName + w.FormKey;

            newCrossbow.Name = newName;
            newCrossbow.BasicStats!.Weight = newCrossbow.BasicStats.Weight * 1.2f;
            newCrossbow.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            newCrossbow.Keywords.Add(kw);
            if (modfn != null) modfn(newCrossbow);
            return newCrossbow;
        }

        private ConstructibleObject CreateEnhancedCrossbowCraftingRecipe(IWeaponGetter oldWeapon, Weapon newWeapon, WeaponMaterial wm, int numKits, 
            params FormLink<IPerkGetter>[] perks)
        {
            var cobj = Patch.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixWeapon + SPrefixCrafting + newWeapon.EditorID + newWeapon.FormKey;
            cobj.WorkbenchKeyword.SetTo(CraftingSmithingForge);
            cobj.CreatedObject.SetTo(newWeapon);
            cobj.AddCraftingRequirement(xMACrossbowModificationKit, numKits);
            cobj.AddCraftingRequirement(oldWeapon, 1);

            foreach (var perk in perks) 
                cobj.AddCraftingPerkCondition(perk);
            cobj.AddCraftingInventoryCondition(oldWeapon);
            return cobj;
        }

        private void CreateRefinedSilverWeapon(Weapon w)
        {
            if (!w.HasKeyword(WeapMaterialSilver))
                return;

            var newName = Storage.GetOutputString("Refined") + " " + w.NameOrThrow();

            var nw = Patch.Weapons.DuplicateInAsNewRecord(w);
            nw.Name = newName;
            nw.Description = SWeaponRefinedDesc;
            nw.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            nw.Keywords.Add(xMAWeapMaterialSilverRefined);
            nw.Keywords.Add(WeapMaterialSilver);

            var wm = Storage.GetWeaponMaterial(nw);
            var wt = Storage.GetWeaponType(nw);
            
            ModStats(nw, wt, wm);
            ApplyModifiers(nw);
            
            // Swap properties on silver sword script
            var script = nw.GetOrAddScript(SScriptSilversword);
            script.Properties.Add(new ScriptObjectProperty()
            {
                Name = SScriptApplyperkProperty,
                Flags = ScriptProperty.Flag.Edited,
                Object = xMAWeapMaterialSilverRefined
            });

            if (!Storage.WeaponReforgeExclusions.IsExcluded(w))
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
            MarkForDistribution(w, wm, wt);
        }

        private void MarkForDistribution(Weapon w, WeaponMaterial wm, WeaponType wt)
        {
            _markedForDistribution.Add((w, wm, wt));
        }
        
        private void DistributeWeaponOnLeveledList(
            Dictionary<(WeaponMaterial?, WeaponType?, FormKey), IEnumerable<IndexedEntry<IWeaponGetter>>> lookupSimilar,
            Weapon w, WeaponMaterial wm, WeaponType wt)
        {
            if (w.Data!.Flags.HasFlag(WeaponData.Flag.CantDrop) || w.Data!.Flags.HasFlag(WeaponData.Flag.BoundWeapon))
                return;

            if (Storage.DistributionExclusionsWeaponRegular.IsExcluded(w))
                return;

            var flink = new FormLink<IItemGetter>(w.FormKey);
            bool similarSet = false;
            IWeaponGetter? firstSimilarMatch = default;

            var newItems = new List<LeveledItemEntry>();

            if (!lookupSimilar.TryGetValue((wm, wt, w.ObjectEffect.FormKey), out var similar))
                return;
            
            foreach (var li in similar)
            {
                if (li.Resolved.Data!.Reference.FormKey == w.FormKey)
                    continue;

                if (!similarSet)
                {
                    if (!DoWeaponsContainClasses(w, li.Item))
                        continue;

                    similarSet = true;
                    firstSimilarMatch = li.Item;
                }
                else
                {
                    if (!Equals(li.Item, firstSimilarMatch))
                        continue;
                }

                var lim = Patch.LeveledItems.GetOrAddAsOverride(li.List);
                lim.Entries ??= new ExtendedList<LeveledItemEntry>();
                lim.Entries.Add(new LeveledItemEntry
                {
                    Data = new LeveledItemEntryData
                    {
                        Level = li.Resolved.Data!.Level,
                        Count = li.Resolved.Data!.Count,
                        Reference = new FormLink<IItemGetter>(w.FormKey)
                    }
                });
            }



            

        }

        private bool AreWeaponsSimilar(Weapon w1, WeaponMaterial wm1, WeaponType wt1, IWeaponGetter w2)
        {
            if (WeaponsWithoutMaterialOrType.Contains(w2.FormKey))
                return false;

            var wm2 = Storage.GetWeaponMaterial(w2);
            var wt2 = Storage.GetWeaponType(w2);

            if (wm2?.Type.Data == null || wt2 == null)
                return false;

            return wm1.Type.Data?.TemperingInput == wm2.Type.Data.TemperingInput &&
                   wt1.BaseWeaponType.Equals(wt2.BaseWeaponType) &&
                   DoWeaponsContainClasses(w1, w2) &&
                   Equals(w1.ObjectEffect, w2.ObjectEffect);

        }

        public static bool DoWeaponsContainClasses(IWeaponGetter w1, IWeaponGetter w2)
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
            var cobj = Patch.ConstructibleObjects.AddNew();
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
            var newName = w.NameOrThrow() + "[" + Storage.GetOutputString(SReplica) + "]";
            var nw = Patch.Weapons.DuplicateInAsNewRecord(w);
            nw.EditorID = SPrefixPatcher + SPrefixWeapon + w.EditorID + "Replica" + nw.FormKey;
            nw.Name = newName;
            nw.ObjectEffect.SetToNull();
            nw.EnchantmentAmount = 0;
            return nw;
        }

        private void AddWarforgedCraftingRecipe(Weapon w, Weapon? oldWeapon, WeaponMaterial wm)
        {
            var cobj = Patch.ConstructibleObjects.AddNew();
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
            var newName = Storage.GetOutputString("Warforged") + " " + w.NameOrEmpty();
            var nw = Patch.Weapons.DuplicateInAsNewRecord(w);
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
            var cobj = Patch.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixWeapon + SPrefixTemper + w.EditorID + w.FormKey;
            cobj.WorkbenchKeyword.SetTo(CraftingSmithingSharpeningWheel);
            cobj.CreatedObject.SetTo(w);
            cobj.CreatedObjectCount = 1;

            var ing = wm.Type.Data.TemperingInput;
            if (wm.Type.Data.TemperingInput == null)
            {
                Logger.LogWarning("No tempering item found for {EditorID} will use meltdown product", w.EditorID);
                ing = wm.Type.Data.BreakdownProduct;
            }

            if (ing != null)
            {
                cobj.AddCraftingRequirement(ing, 1);
            }
            else
            {
                Logger.LogWarning("No input found for tempering recipe for {EditorID}", w.EditorID);
            }

            var perk = wm.Type.Data.SmithingPerk;
            if (perk != null)
                cobj.AddCraftingPerkCondition(perk);
        }

        private void AddReforgedCraftingRecipe(Weapon newWeapon, Weapon? oldWeapon, WeaponMaterial wm)
        {
            var cobj = Patch.ConstructibleObjects.AddNew();
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
            var newName = Storage.GetOutputString("Reforged") + " " + w.NameOrEmpty();
            var nw = Patch.Weapons.DuplicateInAsNewRecord(w);
            nw.Name = newName;
            return nw;
        }

        private void ApplyModifiers(Weapon w)
        {
            var modifiers = Storage.GetAllModifiers(w);
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

            var cobj = Patch.ConstructibleObjects.AddNew();
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
            var skillBase = Storage.GetWeaponSkillDamageBase(wt.BaseWeaponType);
            var typeMod = wt.DamageBase;
            var matMod = wm.DamageModifier;
            var typeMult = Storage.GetWeaponSkillDamageMultipler(wt.BaseWeaponType);

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
            var w = Patch.Weapons.GetOrAddAsOverride(wg);
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
                Logger.LogInformation("Weapon {EditorID}: no meltdown recipe generated", w.EditorID);
                return;
            }

            var cobj = Patch.ConstructibleObjects.AddNew();
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
            foreach (var c in Mods.ConstructibleObject().WinningOverrides()
                .Where(c => c.CreatedObject.FormKey == w.FormKey &&
                            c.WorkbenchKeyword.FormKey == CraftingSmithingSharpeningWheel.FormKey)
                .Select(c => Patch.ConstructibleObjects.GetOrAddAsOverride(c)))
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