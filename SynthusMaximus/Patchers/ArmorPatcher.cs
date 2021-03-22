using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;
using Wabbajack.Common;

using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Noggog;
using SynthusMaximus.Data.LowLevel;

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
                            
                        }

                    }
                        
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Name}: error", a.EditorID);
                }
            }
            
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
                    mod.Keywords.Add(Statics.ArmorHeavyLegs);
                if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                    mod.Keywords.Add(Statics.ArmorLightLegs);
            }
            else if (mod.HasKeyword(Skyrim.Keyword.ArmorCuirass))
            {
                if (mod.HasKeyword(Skyrim.Keyword.ArmorHeavy))
                    mod.Keywords.Add(Statics.ArmorHeavyChest);
                if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                    mod.Keywords.Add(Statics.ArmorLightChest);
            }
            else if (mod.HasKeyword(Skyrim.Keyword.ArmorGauntlets))
            {
                if (mod.HasKeyword(Skyrim.Keyword.ArmorHeavy))
                    mod.Keywords.Add(Statics.ArmorHeavyArms);
                if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                    mod.Keywords.Add(Statics.ArmorLightArms);
            }
            else if (mod.HasKeyword(Skyrim.Keyword.ArmorBoots))
            {
                if (mod.HasKeyword(Skyrim.Keyword.ArmorHelmet))
                    mod.Keywords.Add(Statics.ArmorLightHead);
                if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                    mod.Keywords.Add(Statics.ArmorLightHead);
            }
            else if (mod.HasKeyword(Skyrim.Keyword.ArmorShield))
            {
                if (mod.HasKeyword(Skyrim.Keyword.ArmorHeavy))
                    mod.Keywords.Add(Statics.ArmorHeavyShield);
                if (mod.HasKeyword(Skyrim.Keyword.ArmorLight))
                    mod.Keywords.Add(Statics.ArmorLightShield);
            }

            return true;

        }

        private bool MakeClothingMoreExpensive(IArmorGetter a)
        {
            if (a.Value >= Statics.ExpensiveClothingThreshold
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
                $"{Statics.SPrefixPatcher}{Statics.SPrefixClothing}{Statics.SPrefixMeltdown}{a.EditorID}{a.FormKey.ToString()}";
            
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(a), inputNum);
            cobj.AddCraftingInventoryCondition(new FormLink<IItemGetter>(a));
            cobj.AddCraftingPerkCondition(Statics.PerkSmithingMeltdown);
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