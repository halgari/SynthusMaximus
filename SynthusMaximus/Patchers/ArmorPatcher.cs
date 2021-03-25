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
    public class ArmorPatcher : IPatcher
    {
        private ILogger<ArmorPatcher> _logger;
        private DataStorage _storage;
        private IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
        private ConcurrentHashSet<FormKey> _armorWithNoMaterialOrType = new();  

        public ArmorPatcher(ILogger<ArmorPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            _logger = logger;
            _storage = storage;
            _state = state;
            _logger.LogInformation("ArmorPatcher initialized");
        }
        
        public void RunPatcher()
        {
            var addRecord = false;

            var armors = _state.LoadOrder.PriorityOrder.Armor().WinningOverrides().ToArray();

            _logger.LogInformation("num armors: {Length}", armors.Length);

            foreach (var a in armors)
            {
                _logger.LogTrace("{Name}: started patching", a.EditorID);
                try
                {
                    if (!ShouldPatch(a))
                    {
                        _logger.LogInformation("{Name}: Ingored", a.EditorID);
                        continue;
                    }
                    
                    // do clothing specific stuff, then skip to next armor
                    if (DataStorage.IsClothing(a))
                    {
                        if (_storage.UseWarrior)
                        {
                            AddClothingMeltdownRecipe(a);
                            continue;
                        }

                        
                        if (_storage.UseThief)
                        {
                            if (MakeClothingMoreExpensive(a))
                            {
                                continue;
                            }
                        }
                    }
                    
                    var am = _storage.GetArmorMaterial(a);
                    if (am == null)
                    {
                        if (!DataStorage.IsJewelry(a))
                        {
                            _armorWithNoMaterialOrType.Add(a.FormKey);
                            _logger.LogInformation("{Name}: no material", a.EditorID);
                        }
                        continue;
                    }
                    
                    _logger.LogInformation("{EditorID} - material Found", a.EditorID);
                    
                    // General changes
                    addRecord |= AddSpecificKeyword(a, am);

                    // changes only used when running the warrior module
                    if (_storage.UseWarrior)
                    {
                        addRecord |= addRecord | SetArmorValue(a, am);

                        if (!_storage.IsArmorExcludedReforged(a))
                        {
                            AddMeltdownRecipe(a, am);
                        }

                        if (!_storage.IsArmorExcludedReforged(a) && !DataStorage.IsClothing(a) &&
                            !DataStorage.IsJewelry(a))
                        {
                            var patched = _state.PatchMod.Armors.GetOrAddAsOverride(a);
                            var reforged = CreateReforgedArmor(patched, am);
                            ApplyArmorModifiers(reforged);
                            AddTemperingRecipe(reforged, am);
                            CreateReforgedCraftingRecipe(reforged, a, am);
                            AddMeltdownRecipe(reforged, am);
                            
                            var warforged = CreateWarforgedArmor(reforged, am);
                            ApplyArmorModifiers(warforged);
                            AddTemperingRecipe(warforged, am);
                            CreateWarforgedCraftingRecipe(warforged, reforged, am);
                            AddMeltdownRecipe(warforged, am);


                        }

                        DoCopycat(a, am);

                    }

                    if (_storage.UseThief)
                    {
                        addRecord |= AddMasqueradeKeyword(a);
                        DoQualityLeather(a, am);
                    }

                    if (_storage.UseWarrior)
                    {
                        ApplyArmorModifiers(a);
                    }
                        
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Name}: error", a.EditorID);
                }
            }
            
        }

        private bool DoQualityLeather(IArmorGetter a, ArmorMaterial am)
        {
            if (!a.HasKeyword(ArmorMaterialLeather))
                return false;

            var craftingRecipies = GetCraftingRecipes(a);
            if (!craftingRecipies.Any())
            {
                _logger.LogInformation("{EditorID} : Leather material, but no crafting recipe. No quality leather variant created", a.EditorID);
                return false;
            }

            var temperingRecipes = GetTempreingRecipes(a);

            var qa = CreateQualityArmorVariant(a);
            CreateQualityArmorRecipe(craftingRecipies, qa);
            CreateQualityArmorRecipe(temperingRecipes, qa);
            
            AddTemperingRecipe(qa, am);
            AddMeltdownRecipe(qa, am);

            if (_storage.UseWarrior)
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
                var newRecipe = _state.PatchMod.ConstructibleObjects.DuplicateInAsNewRecord(c);
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

            var newName = name + " [" + _storage.GetOutputString(SQuality) + "]";

            var newArmor = _state.PatchMod.Armors.DuplicateInAsNewRecord(a);
            newArmor.Name = newName;
            ApplyArmorModifiers(newArmor);
            return newArmor;
        }

        private IEnumerable<IConstructibleObjectGetter> GetCraftingRecipes(IArmorGetter armorGetter)
        {
            return _state.LoadOrder.PriorityOrder
                .ConstructibleObject()
                .WinningOverrides()
                .Where(c => c.CreatedObject.FormKey == armorGetter.FormKey)
                .Where(c => c.WorkbenchKeyword.FormKey == CraftingSmithingForge.FormKey);
        }
        
        private IEnumerable<IConstructibleObjectGetter> GetTempreingRecipes(IArmorGetter armorGetter)
        {
            return _state.LoadOrder.PriorityOrder
                .ConstructibleObject()
                .WinningOverrides()
                .Where(c => c.CreatedObject.FormKey == armorGetter.FormKey)
                .Where(c => c.WorkbenchKeyword.FormKey == CraftingSmithingArmorTable.FormKey);
        }

        private bool AddMasqueradeKeyword(IArmorGetter a)
        {
            var ar = _state.PatchMod.Armors.GetOrAddAsOverride(a);
            ar.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            ar.Keywords.AddRange(_storage.GetArmorMasqueradeKeywords(a));
            return true;
        }

        private bool DoCopycat(IArmorGetter a, ArmorMaterial am)
        {
            if (!a.HasKeyword(DaedricArtifact))
                return false;

            var newArmor = CreateCopycatArmor(a);
            CreateCopycatCraftingRecipe(newArmor, a, am);
            
            if (_storage.UseWarrior && !DataStorage.IsJewelry(a) && !DataStorage.IsClothing(a))
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
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
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
            if (!a.Name!.TryLookup(Language.English, out var name))
                throw new InvalidDataException("Could not get English name");

            var newName = name + "[" + _storage.GetOutputString(SReplica) + "]";
            var newArmor = _state.PatchMod.Armors.DuplicateInAsNewRecord(a);
            newArmor.EditorID = SPrefixPatcher + SPrefixArmor + newName + a.FormKey;
            newArmor.Name = newName;
            newArmor.ObjectEffect.SetToNull();
            ApplyArmorModifiers(newArmor);
            return newArmor;
        }

        private Armor CreateReforgedArmor(IArmorGetter a, ArmorMaterial am)
        {
            if (!a.Name!.TryLookup(Language.English, out var name))
                throw new InvalidDataException("Can't get english name");

            var newname = _storage.GetOutputString(SReforged) + " " + name;

            var newArmor = _state.PatchMod.Armors.DuplicateInAsNewRecord(a);
            newArmor.Name = newname;
            newArmor.EditorID = SPrefixPatcher + SPrefixArmor + newname + a.FormKey;

            return newArmor;

        }
        
        private Armor CreateWarforgedArmor(IArmorGetter a, ArmorMaterial am)
        {
            if (!a.Name!.TryLookup(Language.English, out var name))
                throw new InvalidDataException("Can't get english name");

            var newname = _storage.GetOutputString(SWarforged) + " " + name;

            var newArmor = _state.PatchMod.Armors.DuplicateInAsNewRecord(a);
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
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
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
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
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
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
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
            var ar = _state.PatchMod.Armors.GetOrAddAsOverride(a);
            
            foreach (var m in _storage.GetArmorModifiers(a))
            {
                ar.Weight *= m.FactorWeight;
                ar.Value = (uint)(ar.Value * m.FactorValue);
                ar.ArmorRating *= m.FactorArmor;
            }
        }

        private void AddMeltdownRecipe(IArmorGetter a, ArmorMaterial am)
        {
            var meltdownDefintion = am.Type.Data;
            var requiredPerk = meltdownDefintion.SmithingPerk;
            var output = meltdownDefintion.BreakdownProduct;
            var benchKW = meltdownDefintion.BreakdownStation;

            var inputNum = 1;
            var outputNum = _storage.GetArmorMeltdownOutput(a);


            if (output == default || outputNum <= 0 || benchKW == default)
            {
                _logger.LogInformation("{EditorID}: no meltdown recipe generated", a.EditorID);
                return;
            }
            
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
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
            var newArmorValue = (am.ArmorBase * _storage.GetArmorSlotMultiplier(a));
            if (original != newArmorValue && newArmorValue > 0)
            {
                var newRecord = _state.PatchMod.Armors.GetOrAddAsOverride(a);
                newRecord.ArmorRating = newArmorValue;
            }
            else if (newArmorValue < 0)
            {
                _logger.LogWarning("{EditorID}: Failed ot patch armor rating", a.EditorID);
            }

            return false;
        }

        private bool AddSpecificKeyword(IArmorGetter a, ArmorMaterial am)
        {
            var mod = _state.PatchMod.Armors.GetOrAddAsOverride(a);
            
            if (mod.HasKeyword(ArmorHeavy))
                mod.Keywords!.Remove(ArmorHeavy);
            if (mod.HasKeyword(ArmorLight))
                mod.Keywords!.Remove(ArmorLight);

            mod.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();

            switch (am.Class)
            {
                case ArmorClass.Light :
                    mod.Keywords!.Add(ArmorLight);
                    mod.BodyTemplate!.ArmorType = ArmorType.LightArmor;
                    break;
                case ArmorClass.Heavy:
                    mod.Keywords!.Add(ArmorHeavy);
                    mod.BodyTemplate!.ArmorType = ArmorType.HeavyArmor;
                    break;
                case ArmorClass.Both:
                    mod.Keywords!.Add(ArmorLight);
                    mod.Keywords!.Add(ArmorHeavy);
                    break;
                case ArmorClass.Undefined:
                    return true;
                default:
                    return true;
            }

            if (mod.HasKeyword(ArmorBoots))
            {
                if (mod.HasKeyword(ArmorHeavy))
                    mod.Keywords.Add(xMAArmorHeavyLegs);
                if (mod.HasKeyword(ArmorLight))
                    mod.Keywords.Add(xMAArmorLightLegs);
            }
            else if (mod.HasKeyword(ArmorCuirass))
            {
                if (mod.HasKeyword(ArmorHeavy))
                    mod.Keywords.Add(xMAArmorHeavyChest);
                if (mod.HasKeyword(ArmorLight))
                    mod.Keywords.Add(xMAArmorLightChest);
            }
            else if (mod.HasKeyword(ArmorGauntlets))
            {
                if (mod.HasKeyword(ArmorHeavy))
                    mod.Keywords.Add(xMAArmorHeavyArms);
                if (mod.HasKeyword(ArmorLight))
                    mod.Keywords.Add(xMAArmorLightArms);
            }
            else if (mod.HasKeyword(ArmorBoots))
            {
                if (mod.HasKeyword(ArmorHelmet))
                    mod.Keywords.Add(xMAArmorHeavyHead);
                if (mod.HasKeyword(ArmorLight))
                    mod.Keywords.Add(xMAArmorLightHead);
            }
            else if (mod.HasKeyword(ArmorShield))
            {
                if (mod.HasKeyword(ArmorHeavy))
                    mod.Keywords.Add(xMAArmorHeavyShield);
                if (mod.HasKeyword(ArmorLight))
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
                var armor = _state.PatchMod.Armors.GetOrAddAsOverride(a);
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
            var outputNum = _storage.GetArmorMeltdownOutput(a);
            
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID =
                $"{SPrefixPatcher}{SPrefixClothing}{SPrefixMeltdown}{a.EditorID}{a.FormKey.ToString()}";
            
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(a), inputNum);
            cobj.AddCraftingInventoryCondition(new FormLink<IItemGetter>(a));
            cobj.AddCraftingPerkCondition(xMASMIMeltdown);
            cobj.CreatedObject.SetTo(output);
            cobj.CreatedObjectCount = outputNum;
            cobj.WorkbenchKeyword.SetTo(benchKW);
            _logger.LogInformation("{EditorID}: Finished adding meltdown recipe", a.EditorID);
        }

        private bool ShouldPatch(IArmorGetter a)
        {
            if (!a.TemplateArmor.IsNull)
            {
                _logger.LogTrace("{Name}: Has template", a.EditorID);
                return false;
            }
            else if (DataStorage.IsJewelry(a))
            {
                _logger.LogTrace("{Name}: Is Jewelery", a.EditorID);
                return false;
            }
            else if (_armorWithNoMaterialOrType.Contains(a.FormKey))
            {
                _logger.LogTrace("{Name}: previously excluded", a.EditorID);
                return false;
            }

            var am = _storage.GetArmorMaterial(a);

            return true;
        }
    }
}