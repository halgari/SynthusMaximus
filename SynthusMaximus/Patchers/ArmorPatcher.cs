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
using SynthusMaximus.Data.Enums;
using SynthusMaximus.Data.LowLevel;
using static SynthusMaximus.Data.Statics;
using Armor = Mutagen.Bethesda.Skyrim.Armor;

namespace SynthusMaximus.Patchers
{
    public class ArmorPatcher
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
        }

        public void RunChanges()
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
                            CreateWarforgedArmor(patched, reforged, am);

                        }

                    }
                        
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Name}: error", a.EditorID);
                }
            }
            
        }

        private Armor CreateReforgedArmor(IArmorGetter a, ArmorMaterial am)
        {
            if (!a.Name!.TryLookup(Language.English, out var name))
                throw new InvalidDataException("Can't get english name");

            var newname = _storage.GetOutputString(SReforged) + " " + name;

            var newArmor = _state.PatchMod.Armors.DuplicateInAsNewRecord(a);
            newArmor.Name = newname;
            newArmor.EditorID = SPrefixPatcher + SPrefixArmor + newname + a.FormKey;

            ApplyArmorModifiers(newArmor);
            AddTemperingRecipe(newArmor, am);
            CreateReforgedCraftingRecipe(newArmor, a, am);
            AddMeltdownRecipe(newArmor, am);

            return newArmor;

        }
        
        private Armor CreateWarforgedArmor(IArmorGetter a, IArmorGetter reforgedArmor, ArmorMaterial am)
        {
            if (!a.Name!.TryLookup(Language.English, out var name))
                throw new InvalidDataException("Can't get english name");

            var newname = _storage.GetOutputString(SWarforged) + " " + name;

            var newArmor = _state.PatchMod.Armors.DuplicateInAsNewRecord(a);
            newArmor.Name = newname;
            newArmor.EditorID = SPrefixPatcher + SPrefixArmor + newname + a.FormKey;

            newArmor.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            newArmor.Keywords.Add(Skyrim.Keyword.MagicDisallowEnchanting);
            newArmor.Keywords.Add(SmithingWarforgedArmor);
            newArmor.ObjectEffect.SetTo(EnchSmithingWarforgedArmor);

            ApplyArmorModifiers(newArmor);
            AddTemperingRecipe(newArmor, am);
            CreateWarforgedCraftingRecipe(newArmor, reforgedArmor, am);
            AddMeltdownRecipe(newArmor, am);

            return newArmor;

        }

        private void CreateWarforgedCraftingRecipe(IArmorGetter newArmor, IArmorGetter oldArmor, ArmorMaterial am)
        {
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixArmor + newArmor.EditorID + oldArmor.FormKey;

            var matdesc = am.MaterialTemper.GetDefinition();
            var materialPerk = matdesc.SmithingPerk;
            var input = matdesc.TemperingInput;
            
            cobj.WorkbenchKeyword.SetTo(Skyrim.Keyword.CraftingSmithingForge);
            cobj.CreatedObject.SetTo(newArmor);
            
            if (input != null)
                cobj.AddCraftingRequirement(input, 5);
            
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(oldArmor.FormKey), 1);
            

            cobj.AddCraftingPerkCondition(SmithingMasteryWarforged);
            
            if (materialPerk != null)
                cobj.AddCraftingPerkCondition(materialPerk);
            

        }

        private void CreateReforgedCraftingRecipe(IArmorGetter newArmor, IArmorGetter oldArmor, ArmorMaterial am)
        {
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixArmor + newArmor.EditorID + oldArmor.FormKey;

            var matdesc = am.MaterialTemper.GetDefinition();
            var materialPerk = matdesc.SmithingPerk;
            var input = matdesc.TemperingInput;
            cobj.WorkbenchKeyword.SetTo(Skyrim.Keyword.CraftingSmithingForge);
            cobj.CreatedObject.SetTo(newArmor);
            
            if (input != null)
                cobj.AddCraftingRequirement(input, 2);
            

            cobj.AddCraftingPerkCondition(SmithingArmorer);
            
            if (materialPerk != null)
                cobj.AddCraftingPerkCondition(materialPerk);
            
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(oldArmor.FormKey), 1);
        }

        private void AddTemperingRecipe(IArmorGetter a, ArmorMaterial am)
        {
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID = SPrefixPatcher + SPrefixArmor + SPrefixTemper + a.EditorID +
                            a.FormKey;

            var materialDefinition = am.MaterialTemper.GetDefinition();
            var temperInput = materialDefinition.TemperingInput;
            var perk = materialDefinition.SmithingPerk;

            cobj.WorkbenchKeyword.SetTo(Skyrim.Keyword.CraftingSmithingArmorTable);
            cobj.CreatedObject.SetTo(a);
            cobj.CreatedObjectCount = 1;
            
            if (temperInput != null)
                cobj.AddCraftingRequirement(temperInput, 1);

            if (perk != null) 
                cobj.AddCraftingPerkCondition(perk);
        }

        private void ApplyArmorModifiers(Armor a)
        {
            foreach (var m in _storage.GetArmorModifiers(a))
            {
                a.Weight *= m.FactorWeight;
                a.Value = (uint)(a.Value * m.FactorValue);
                a.ArmorRating *= m.FactorArmor;
            }
        }

        private void AddMeltdownRecipe(IArmorGetter a, ArmorMaterial am)
        {
            var meltdownDefintion = am.MaterialMeltdown.GetDefinition();
            var requiredPerk = meltdownDefintion.SmithingPerk;
            var output = meltdownDefintion.MeltdownProduct;
            var benchKW = meltdownDefintion.MeltdownCraftingStation;

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
            cobj.CreatedObject.SetTo(output);
            cobj.CreatedObjectCount = outputNum;
            cobj.WorkbenchKeyword.SetTo(benchKW);
            
            if (requiredPerk != default)
                cobj.AddCraftingPerkCondition(requiredPerk);
            
            cobj.AddCraftingInventoryCondition(new FormLink<IItemGetter>(a));
            cobj.AddCraftingPerkCondition(PerkSmithingMeltdown);
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
            
            if (mod.HasKeyword(Skyrim.Keyword.ArmorHeavy))
                mod.Keywords!.Remove(Skyrim.Keyword.ArmorHeavy);
            if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                mod.Keywords!.Remove(Skyrim.Keyword.ArmorLight);

            mod.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();

            switch (am.Type)
            {
                case ArmorMaterial.ArmorType.LIGHT:
                    mod.Keywords!.Add(Skyrim.Keyword.ArmorLight);
                    mod.BodyTemplate!.ArmorType = ArmorType.LightArmor;
                    break;
                case ArmorMaterial.ArmorType.HEAVY:
                    mod.Keywords!.Add(Skyrim.Keyword.ArmorHeavy);
                    mod.BodyTemplate!.ArmorType = ArmorType.HeavyArmor;
                    break;
                case ArmorMaterial.ArmorType.BOTH:
                    mod.Keywords!.Add(Skyrim.Keyword.ArmorLight);
                    mod.Keywords!.Add(Skyrim.Keyword.ArmorHeavy);
                    break;
                case ArmorMaterial.ArmorType.UNDEFINED:
                    return true;
                default:
                    return true;
            }

            if (mod.HasKeyword(Skyrim.Keyword.ArmorBoots))
            {
                if (mod.HasKeyword(Skyrim.Keyword.ArmorHeavy))
                    mod.Keywords.Add(ArmorHeavyLegs);
                if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                    mod.Keywords.Add(ArmorLightLegs);
            }
            else if (mod.HasKeyword(Skyrim.Keyword.ArmorCuirass))
            {
                if (mod.HasKeyword(Skyrim.Keyword.ArmorHeavy))
                    mod.Keywords.Add(ArmorHeavyChest);
                if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                    mod.Keywords.Add(ArmorLightChest);
            }
            else if (mod.HasKeyword(Skyrim.Keyword.ArmorGauntlets))
            {
                if (mod.HasKeyword(Skyrim.Keyword.ArmorHeavy))
                    mod.Keywords.Add(ArmorHeavyArms);
                if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                    mod.Keywords.Add(ArmorLightArms);
            }
            else if (mod.HasKeyword(Skyrim.Keyword.ArmorBoots))
            {
                if (mod.HasKeyword(Skyrim.Keyword.ArmorHelmet))
                    mod.Keywords.Add(ArmorLightHead);
                if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                    mod.Keywords.Add(ArmorLightHead);
            }
            else if (mod.HasKeyword(Skyrim.Keyword.ArmorShield))
            {
                if (mod.HasKeyword(Skyrim.Keyword.ArmorHeavy))
                    mod.Keywords.Add(ArmorHeavyShield);
                if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                    mod.Keywords.Add(ArmorLightShield);
            }

            return true;

        }

        private bool MakeClothingMoreExpensive(IArmorGetter a)
        {
            if (a.Value >= ExpensiveClothingThreshold
                && a.HasKeyword(Skyrim.Keyword.ClothingBody)
                && !a.HasKeyword(Skyrim.Keyword.ClothingRich))
            {
                var armor = _state.PatchMod.Armors.GetOrAddAsOverride(a);
                armor.Keywords!.Add(Skyrim.Keyword.ClothingRich);
                return true;
            }

            return false;
        }

        private void AddClothingMeltdownRecipe(IArmorGetter a)
        {
            var output = Skyrim.MiscItem.LeatherStrips;
            var benchKW = Skyrim.Keyword.CraftingTanningRack;

            var inputNum = 1;
            var outputNum = _storage.GetArmorMeltdownOutput(a);
            
            var cobj = _state.PatchMod.ConstructibleObjects.AddNew();
            cobj.EditorID =
                $"{SPrefixPatcher}{SPrefixClothing}{SPrefixMeltdown}{a.EditorID}{a.FormKey.ToString()}";
            
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(a), inputNum);
            cobj.AddCraftingInventoryCondition(new FormLink<IItemGetter>(a));
            cobj.AddCraftingPerkCondition(PerkSmithingMeltdown);
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