﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Perk;

namespace SynthusMaximus.Data.Enums
{
    public enum BaseMaterialArmor
    {
        [EnumMember(Value = "ADVANCED")] Advanced,
        [EnumMember(Value = "NONE")] None,
        [EnumMember(Value = "IRON")] Iron,
        [EnumMember(Value = "STEEL")] Steel,
        [EnumMember(Value = "DWARVEN")] Dwarven,
        [EnumMember(Value = "FALMER")] Falmer,
        [EnumMember(Value = "ORCISH")] Orcish,
        [EnumMember(Value = "STEELPLATE")] SteelPlate,
        [EnumMember(Value = "EBONY")] Ebony,
        [EnumMember(Value = "DRAGONPLATE")] DragonPlate,
        [EnumMember(Value = "DAEDRIC")] Daedric,
        [EnumMember(Value = "FUR")] Fur,
        [EnumMember(Value = "HIDE")] Hide,
        [EnumMember(Value = "LEATHER")] Leather,
        [EnumMember(Value = "ELVEN")] Elven,
        [EnumMember(Value = "SCALED")] Scaled,
        [EnumMember(Value = "GLASS")] Glass,
        [EnumMember(Value = "DRAGONSCALE")] DragonScale,
        [EnumMember(Value = "STALHRIM_HEAVY")] StalhrimHeavy,
        [EnumMember(Value = "STALHRIM_LIGHT")] StalhrimLight,
        [EnumMember(Value = "NORDIC_HEAVY")] NordicHeavy,
        [EnumMember(Value = "BONEMOLD_HEAVY")] BoneMoldHeavy,
        [EnumMember(Value = "CHITIN")] Chitin,
        [EnumMember(Value = "SILVER")] Silver,
        [EnumMember(Value = "GOLD")] Gold,
        [EnumMember(Value = "Wood")] Wood,


    };

    public class BaseMaterialArmorDefinition
    {
        public static Dictionary<BaseMaterialArmor, BaseMaterialArmorDefinition> ByEnum { get; }

        public static List<BaseMaterialArmorDefinition> Registry { get; } = new()
        {
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Advanced, AdvancedArmors, IngotCorundum, CraftingSmelter,
                IngotCorundum),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.None, default, default, default, default),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Iron, default, IngotIron, CraftingSmelter, IngotIron),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Steel, SteelSmithing, IngotSteel, CraftingSmelter, IngotSteel)
        };
        
        static BaseMaterialArmorDefinition()
        {
            ByEnum = Registry.ToDictionary(e => e.BaseMaterialArmor);
            foreach (var itm in Enum.GetValues<BaseMaterialArmor>())
            {
                if (!ByEnum.ContainsKey(itm))
                    throw new InvalidDataException($"Missing enum data for {itm}");
            }
        }

        public BaseMaterialArmor BaseMaterialArmor { get; }
        public IFormLink<IPerkGetter>? SmithingPerk { get; }
        public IFormLink<IConstructibleGetter>? MeltdownProduct { get; }
        public IFormLink<IKeywordGetter>? MeltdownCraftingStation { get; }
        public IFormLink<IItemGetter>? TemperingInput { get; }

        public BaseMaterialArmorDefinition(
            BaseMaterialArmor baseMaterial,
            IFormLink<IPerkGetter>? relatedSmithingPerk,
            IFormLink<IConstructibleGetter>? relatedMeltdownProduct,
            IFormLink<IKeywordGetter>? relatedMeltdownCraftingStation, 
            IFormLink<IItemGetter>? relatedTemperingInput)
        {
            BaseMaterialArmor = baseMaterial;
            SmithingPerk = relatedSmithingPerk;
            MeltdownProduct = relatedMeltdownProduct;
            MeltdownCraftingStation = relatedMeltdownCraftingStation;
            TemperingInput = relatedTemperingInput;
        }
    }
    
    public static class BaseMaterialExtensions
    {
        public static BaseMaterialArmorDefinition GetDefinition(this BaseMaterialArmor armor)
        {
            return BaseMaterialArmorDefinition.ByEnum[armor];
        }
    
    }
    
}