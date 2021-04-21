using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;
using Wabbajack.Common;

using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Noggog;
using SynthusMaximus.Data.DTOs.Armor;
using SynthusMaximus.Data.Enums;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static SynthusMaximus.Data.Statics;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Perk;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.MiscItem;
using Armor = Mutagen.Bethesda.Skyrim.Armor;

namespace SynthusMaximus.Patchers
{
    public class ArmorPatcher : APatcher<ArmorPatcher>
    {
        private ConcurrentHashSet<FormKey> _armorWithNoMaterialOrType = new();  
        
        
        public ArmorPatcher(ILogger<ArmorPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }


        protected override void RunPatcherInner()
        {
            var addRecord = false;

            var armors = State.LoadOrder.PriorityOrder.Armor().WinningOverrides().ToArray();

            foreach (var a in armors)
            {
                try
                {
                    if (!ShouldPatch(a))
                    { 
                        Ignore(a, "Should not patch");
                        continue;
                    }
                    
                    // do clothing specific stuff, then skip to next armor
                    if (Storage.IsClothing(a))
                    {
                        if (Storage.UseWarrior)
                        {
                            AddClothingMeltdownRecipe(a);
                            continue;
                        }

                        
                        if (Storage.UseThief)
                        {
                            if (MakeClothingMoreExpensive(a))
                            {
                                continue;
                            }
                        }
                    }
                    
                    var am = Storage.GetArmorMaterial(a);
                    if (am?.Type.Data == null)
                    {
                        if (!Storage.IsJewelry(a))
                        {
                            _armorWithNoMaterialOrType.Add(a.FormKey);
                            Ignore(a, "No material");
                        }
                        continue;
                    }
                    
                    
                    // General changes
                    addRecord |= AddSpecificKeyword(a, am);

                    // changes only used when running the warrior module
                    if (Storage.UseWarrior)
                    {
                        addRecord |= addRecord | SetArmorValue(a, am);

                        if (!Storage.ArmorReforgeExclusions.Matches(a))
                        {
                            AddMeltdownRecipe(a, am);
                        }

                        if (!Storage.ArmorReforgeExclusions.Matches(a) && !Storage.IsClothing(a) &&
                            !Storage.IsJewelry(a))
                        {
                            var patched = State.PatchMod.Armors.GetOrAddAsOverride(a);
                            var reforged = CreateReforgedArmor(patched, am);
                            ApplyArmorModifiers(reforged);
                            AddTemperingRecipe(reforged, am);
                            CreateReforgedCraftingRecipe(reforged, a, am);
                            AddMeltdownRecipe(reforged, am);
                            
                            var warforged = CreateWarforgedArmor(patched, am);
                            ApplyArmorModifiers(warforged);
                            AddTemperingRecipe(warforged, am);
                            CreateWarforgedCraftingRecipe(warforged, reforged, am);
                            AddMeltdownRecipe(warforged, am);

                            

                        }

                        DoCopycat(a, am);

                    }

                    if (Storage.UseThief)
                    {
                        addRecord |= AddMasqueradeKeyword(a);
                        DoQualityLeather(a, am);
                    }

                    if (Storage.UseWarrior)
                    {
                        ApplyArmorModifiers(a);
                    }
                        
                }
                catch (Exception ex)
                {
                    Failed(ex, a);
                    continue;
                }
                
                Success(a);
            }


        }

        private bool DoQualityLeather(IArmorGetter a, ArmorMaterial am)
        {
            if (!a.HasKeyword(ArmorMaterialLeather))
                return false;

            var craftingRecipies = GetCraftingRecipes(a);
            if (!craftingRecipies.Any())
            {
                Ignore(a, "Leather material, but no crafting recipe found. No quality leather variant created");
                return false;
            }

            var temperingRecipes = GetTempreingRecipes(a);

            var qa = CreateQualityArmorVariant(a);
            CreateQualityArmorRecipe(craftingRecipies, qa);
            CreateQualityArmorRecipe(temperingRecipes, qa);
            
            AddTemperingRecipe(qa, am);
            AddMeltdownRecipe(qa, am);

            if (Storage.UseWarrior)
            {
                DoCopycat(qa, am);

                var qr = CreateReforgedArmor(qa, am);
                CreateReforgedCraftingRecipe(qr, a, am);
                AddTemperingRecipe(qr, am);
                AddMeltdownRecipe(qr, am);

                var qw = CreateWarforgedArmor(qa, am);
                CreateWarforgedCraftingRecipe(qw, qr, am);
                AddTemperingRecipe(qr, am);
                AddMeltdownRecipe(qw, am);
            }

            return true;
        }

        private void CreateQualityArmorRecipe(IEnumerable<IConstructibleObjectGetter> recipes, IArmorGetter a)
        {
            foreach (var c in recipes)
            {
                var newRecipe = State.PatchMod.ConstructibleObjects.DuplicateInAsNewRecord(c);
                newRecipe.EditorID = SPrefixPatcher + a.EditorID + a.FormKey;
                newRecipe.CreatedObject.SetTo(a);
                
                var needsLeatherStrips = false;
                var neesdLeather = false;

                foreach (var i in newRecipe.Items ?? new ExtendedList<ContainerEntry>())
                {
                    if (i.Item.Item.FormKey == Leather01.FormKey)
                    {
                        i.Item.Item.SetTo(xMAWAYQualityLeather);
                        neesdLeather = true;
                    }
                    else if (i.Item.Item.FormKey == LeatherStrips.FormKey)
                    {
                        i.Item.Item.SetTo(xMAWAYQualityLeatherStrips);
                        needsLeatherStrips = true;
                    }
                }
                
                newRecipe.Conditions.Clear();
                newRecipe.AddCraftingPerkCondition(xMASMIMaterialLeather);
                
                if (neesdLeather)
                    newRecipe.AddCraftingInventoryCondition(xMAWAYQualityLeather);
                
                if (needsLeatherStrips)
                    newRecipe.AddCraftingInventoryCondition(xMAWAYQualityLeatherStrips);

            }
        }

        private IArmor CreateQualityArmorVariant(IArmorGetter a)
        {
            if (!a.Name!.TryLookup(Language.English, out var name))
                throw new InvalidDataException("Could not get English name");

            var newName = name + " [" + Storage.GetOutputString(SQuality) + "]";

            var newArmor = State.PatchMod.Armors.DuplicateInAsNewRecord(a);
            newArmor.Name = newName;
            ApplyArmorModifiers(newArmor);
            return newArmor;
        }

        private IEnumerable<IConstructibleObjectGetter> GetCraftingRecipes(IArmorGetter armorGetter)
        {
            return State.LoadOrder.PriorityOrder
                .ConstructibleObject()
                .WinningOverrides()
                .Where(c => c.CreatedObject.FormKey == armorGetter.FormKey)
                .Where(c => c.WorkbenchKeyword.FormKey == CraftingSmithingForge.FormKey);
        }
        
        private IEnumerable<IConstructibleObjectGetter> GetTempreingRecipes(IArmorGetter armorGetter)
        {
            return State.LoadOrder.PriorityOrder
                .ConstructibleObject()
                .WinningOverrides()
                .Where(c => c.CreatedObject.FormKey == armorGetter.FormKey)
                .Where(c => c.WorkbenchKeyword.FormKey == CraftingSmithingArmorTable.FormKey);
        }

        private bool AddMasqueradeKeyword(IArmorGetter a)
        {
            var ar = State.PatchMod.Armors.GetOrAddAsOverride(a);
            ar.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            ar.Keywords.AddRange(Storage.GetArmorMasqueradeKeywords(a));
            return true;
        }

        private bool DoCopycat(IArmorGetter a, ArmorMaterial am)
        {
            if (!a.HasKeyword(DaedricArtifact) && am.Type.Data != default)
                return false;

            var newArmor = CreateCopycatArmor(a);
            CreateCopycatCraftingRecipe(newArmor, a, am);
            
            if (Storage.UseWarrior && !Storage.IsJewelry(a) && !Storage.IsClothing(a))
            {
                AddMeltdownRecipe(newArmor, am);
                var ar = CreateReforgedArmor(newArmor, am);
                CreateReforgedCraftingRecipe(ar, a, am);
                AddMeltdownRecipe(ar, am);

                var aw = CreateWarforgedArmor(ar, am);
                CreateWarforgedCraftingRecipe(aw, ar, am);
                AddMeltdownRecipe(aw, am);
            }

            return true;
        }

        private IConstructibleObjectGetter CreateCopycatCraftingRecipe(IArmorGetter newArmor, IArmorGetter oldArmor, ArmorMaterial am)
        {
            var cobj = State.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixArmor + newArmor.EditorID + oldArmor.FormKey;

            var matdesc = am.Type.Data;

            var materialPerk = matdesc.SmithingPerk;
            var input = matdesc.TemperingInput;
            
            cobj.WorkbenchKeyword.SetTo(CraftingSmithingForge);
            cobj.CreatedObject.SetTo(newArmor);
            cobj.AddCraftingRequirement(xMASMICopycatArtifactEssence, 1);
            
            if (input != null)
                cobj.AddCraftingRequirement(input, 3);
            
            cobj.AddCraftingInventoryCondition(new FormLink<IItemGetter>(oldArmor.FormKey), 1);
            cobj.AddCraftingPerkCondition(xMASMICopycat);
            
            if (materialPerk != null)
                cobj.AddCraftingPerkCondition(materialPerk);
            return cobj;
        }

        private IArmor CreateCopycatArmor(IArmorGetter a)
        {
            var newName = a.NameOrThrow() + " [" + Storage.GetOutputString(SReplica) + "]";
            var newArmor = State.PatchMod.Armors.DuplicateInAsNewRecord(a);
            newArmor.SetEditorID(SPrefixPatcher + SPrefixArmor + newName, a);
            newArmor.Name = newName;
            newArmor.ObjectEffect.SetToNull();
            ApplyArmorModifiers(newArmor);
            return newArmor;
        }

        private Armor CreateReforgedArmor(IArmorGetter a, ArmorMaterial am)
        {
            var newname = Storage.GetOutputString(SReforged) + " " + a.NameOrThrow();

            var newArmor = State.PatchMod.Armors.DuplicateInAsNewRecord(a);
            newArmor.Name = newname;
            newArmor.SetEditorID(SPrefixPatcher + SPrefixArmor + newname, a);

            return newArmor;

        }
        
        private Armor CreateWarforgedArmor(IArmorGetter a, ArmorMaterial am)
        {
            var newname = Storage.GetOutputString(SWarforged) + " " + a.NameOrThrow();

            var newArmor = State.PatchMod.Armors.DuplicateInAsNewRecord(a);
            newArmor.Name = newname;
            newArmor.EditorID = SPrefixPatcher + SPrefixArmor + newname + a.FormKey;

            newArmor.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            newArmor.Keywords.Add(MagicDisallowEnchanting);
            newArmor.Keywords.Add(xMASMIWarforgedArmorKW);
            
            newArmor.ObjectEffect.SetTo(PerkusMaximus_Master.ObjectEffect.xMASMIMasteryWarforgedEnchArmor);

            return newArmor;

        }

        private void CreateWarforgedCraftingRecipe(IArmorGetter newArmor, IArmorGetter oldArmor, ArmorMaterial am)
        {
            var cobj = State.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixArmor + newArmor.EditorID + oldArmor.FormKey;

            var matdesc = am.Type.Data;
            var materialPerk = matdesc.SmithingPerk;
            var input = matdesc.TemperingInput;
            
            cobj.WorkbenchKeyword.SetTo(CraftingSmithingForge);
            cobj.CreatedObject.SetTo(newArmor);
            
            if (input != null)
                cobj.AddCraftingRequirement(input, 5);
            
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(oldArmor.FormKey), 1);
            

            cobj.AddCraftingPerkCondition(xMASMIMasteryWarforged);
            
            if (materialPerk != null)
                cobj.AddCraftingPerkCondition(materialPerk);
            

        }

        private void CreateReforgedCraftingRecipe(IArmorGetter newArmor, IArmorGetter oldArmor, ArmorMaterial am)
        {
            var cobj = State.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixArmor + newArmor.EditorID + oldArmor.FormKey;

            var matdesc = am.Type.Data;
            var materialPerk = matdesc.SmithingPerk;
            var input = matdesc.TemperingInput;
            cobj.WorkbenchKeyword.SetTo(CraftingSmithingForge);
            cobj.CreatedObject.SetTo(newArmor);
            
            if (input != null)
                cobj.AddCraftingRequirement(input, 2);
            

            cobj.AddCraftingPerkCondition(xMASMIArmorer);
            
            if (materialPerk != null)
                cobj.AddCraftingPerkCondition(materialPerk);
            
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(oldArmor.FormKey), 1);
        }

        private void AddTemperingRecipe(IArmorGetter a, ArmorMaterial am)
        {
            var cobj = State.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixArmor + SPrefixTemper + a.EditorID +
                            a.FormKey;

            var materialDefinition = am.Type.Data;
            var temperInput = materialDefinition.TemperingInput;
            var perk = materialDefinition.SmithingPerk;

            cobj.WorkbenchKeyword.SetTo(CraftingSmithingArmorTable);
            cobj.CreatedObject.SetTo(a);
            cobj.CreatedObjectCount = 1;
            
            if (temperInput != null)
                cobj.AddCraftingRequirement(temperInput, 1);

            if (perk != null) 
                cobj.AddCraftingPerkCondition(perk);
        }

        private void ApplyArmorModifiers(IArmorGetter a)
        {
            var ar = State.PatchMod.Armors.GetOrAddAsOverride(a);

            float value = a.Value;
            foreach (var m in Storage.GetArmorModifiers(a))
            {
                ar.Weight *= m.FactorWeight;
                value *= m.FactorValue;
                ar.ArmorRating *= m.FactorArmor;
            }

            // Cast here avoid compounding rounding errors
            ar.Value = (uint)value;
        }

        private void AddMeltdownRecipe(IArmorGetter a, ArmorMaterial am)
        {
            var meltdownDefintion = am.Type.Data;
            var requiredPerk = meltdownDefintion.SmithingPerk;
            var output = meltdownDefintion.BreakdownProduct;
            var benchKW = meltdownDefintion.BreakdownStation;

            var inputNum = 1;
            var outputNum = Storage.GetArmorMeltdownOutput(a);


            if (output == default || outputNum <= 0 || benchKW == default)
            {
                Ignore(a, "No meltdown recipe generated");
                return;
            }
            
            var cobj = State.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID =
                $"{SPrefixPatcher}{SPrefixArmor}{SPrefixMeltdown}{a.EditorID}{a.FormKey.ToString()}";
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(a), inputNum);
            cobj.CreatedObject.SetTo(output.FormKey);
            cobj.CreatedObjectCount = outputNum;
            cobj.WorkbenchKeyword.SetTo(benchKW);
            
            if (requiredPerk != default)
                cobj.AddCraftingPerkCondition(requiredPerk);
            
            cobj.AddCraftingInventoryCondition(new FormLink<IItemGetter>(a));
            cobj.AddCraftingPerkCondition(xMASMIMeltdown);
        }

        private bool SetArmorValue(IArmorGetter a, ArmorMaterial am)
        {
            var original = a.ArmorRating;
            var slotModifier = Storage.GetArmorSlotMultiplier(a);
            if (slotModifier == null)
            {
                Ignore(a, "No armor slot");
                return false;
            }
            var newArmorValue = (float)(am.ArmorBase * slotModifier!);
            if (original != newArmorValue && newArmorValue > 0)
            {
                var newRecord = State.PatchMod.Armors.GetOrAddAsOverride(a);
                newRecord.ArmorRating = newArmorValue;
            }
            else if (newArmorValue < 0)
            {
                Ignore(a, "Failed to patch armor rating");
            }

            return false;
        }

        private bool AddSpecificKeyword(IArmorGetter a, ArmorMaterial am)
        {
            var mod = State.PatchMod.Armors.GetOrAddAsOverride(a);
            var kws = mod.Keywords?.ToHashSet() ?? new HashSet<IFormLinkGetter<IKeywordGetter>>();
            
            if (kws.Contains(ArmorHeavy))
                kws.Remove(ArmorHeavy);
            if (kws.Contains(ArmorLight))
                kws.Remove(ArmorLight);

            mod.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();

            switch (am.Class)
            {
                case ArmorClass.Light :
                    kws.Add(ArmorLight);
                    mod.BodyTemplate!.ArmorType = ArmorType.LightArmor;
                    break;
                case ArmorClass.Heavy:
                    kws.Add(ArmorHeavy);
                    mod.BodyTemplate!.ArmorType = ArmorType.HeavyArmor;
                    break;
                case ArmorClass.Both:
                    kws.Add(ArmorLight);
                    kws.Add(ArmorHeavy);
                    break;
                case ArmorClass.Undefined:
                    return true;
                default:
                    return true;
            }

            if (kws.Contains(ArmorBoots))
            {
                if (kws.Contains(ArmorHeavy))
                    mod.Keywords.Add(xMAArmorHeavyLegs);
                if (kws.Contains(ArmorLight))
                    mod.Keywords.Add(xMAArmorLightLegs);
            }
            else if (kws.Contains(ArmorCuirass))
            {
                if (kws.Contains(ArmorHeavy))
                    mod.Keywords.Add(xMAArmorHeavyChest);
                if (kws.Contains(ArmorLight))
                    mod.Keywords.Add(xMAArmorLightChest);
            }
            else if (kws.Contains(ArmorGauntlets))
            {
                if (kws.Contains(ArmorHeavy))
                    mod.Keywords.Add(xMAArmorHeavyArms);
                if (kws.Contains(ArmorLight))
                    mod.Keywords.Add(xMAArmorLightArms);
            }
            else if (kws.Contains(ArmorHelmet))
            {
                if (kws.Contains(ArmorHeavy))
                    mod.Keywords.Add(xMAArmorHeavyHead);
                if (kws.Contains(ArmorLight))
                    mod.Keywords.Add(xMAArmorLightHead);
            }
            else if (kws.Contains(ArmorShield))
            {
                if (kws.Contains(ArmorHeavy))
                    mod.Keywords.Add(xMAArmorHeavyShield);
                if (kws.Contains(ArmorLight))
                    mod.Keywords.Add(xMAArmorLightShield);
            }

            return true;

        }

        private bool MakeClothingMoreExpensive(IArmorGetter a)
        {
            if (a.Value >= ExpensiveClothingThreshold
                && a.HasKeyword(ClothingBody)
                && !a.HasKeyword(ClothingRich))
            {
                var armor = State.PatchMod.Armors.GetOrAddAsOverride(a);
                armor.Keywords!.Add(ClothingRich);
                return true;
            }

            return false;
        }

        private void AddClothingMeltdownRecipe(IArmorGetter a)
        {
            var output = Skyrim.MiscItem.LeatherStrips;
            var benchKW = CraftingTanningRack;

            var inputNum = 1;
            var outputNum = Storage.GetArmorMeltdownOutput(a);
            
            var cobj = State.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID =
                $"{SPrefixPatcher}{SPrefixClothing}{SPrefixMeltdown}{a.EditorID}{a.FormKey.ToString()}";
            
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(a), inputNum);
            cobj.AddCraftingInventoryCondition(new FormLink<IItemGetter>(a));
            cobj.AddCraftingPerkCondition(xMASMIMeltdown);
            cobj.CreatedObject.SetTo(output);
            cobj.CreatedObjectCount = outputNum;
            cobj.WorkbenchKeyword.SetTo(benchKW);
        }

        private bool ShouldPatch(IArmorGetter a)
        {
            if (!a.TemplateArmor.IsNull)
            {
                return false;
            }
            else if (Storage.IsJewelry(a))
            {
                return false;
            }
            else if (_armorWithNoMaterialOrType.Contains(a.FormKey))
            {
                return false;
            }
            
            return true;
        }

    }
}