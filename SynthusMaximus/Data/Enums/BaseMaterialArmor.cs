using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Dragonborn.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Perk;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Perk;

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
        [EnumMember(Value = "WOOD")] Wood,


    };

    public class BaseMaterialArmorDefinition
    {
        public static Dictionary<BaseMaterialArmor, BaseMaterialArmorDefinition> ByEnum { get; }

        public static List<BaseMaterialArmorDefinition> Registry { get; } = new()
        {
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Advanced, AdvancedArmors, IngotCorundum, CraftingSmelter,
                IngotCorundum),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.None, default, default, default, default),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Iron, default, IngotIron),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Steel, SteelSmithing, IngotSteel),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Dwarven, DwarvenSmithing, IngotDwarven),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Falmer, AdvancedArmors, ChaurusChitin),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Orcish, OrcishSmithing, IngotOrichalcum),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.SteelPlate, AdvancedArmors, IngotSteel),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Ebony, EbonySmithing, IngotEbony),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.DragonPlate, DragonArmor, DragonBone),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Daedric, DaedricSmithing, IngotEbony),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Fur, default, LeatherStrips, CraftingTanningRack, LeatherStrips),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Hide, default, LeatherStrips, CraftingTanningRack, LeatherStrips),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Leather, xMASMIMaterialLeather, LeatherStrips, CraftingTanningRack, LeatherStrips),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Elven, ElvenSmithing, IngotIMoonstone),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Scaled, AdvancedArmors, IngotCorundum),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Glass, GlassSmithing, IngotMalachite),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.DragonScale, DragonArmor, DragonScales),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.StalhrimHeavy, EbonySmithing, DLC2OreStalhrim),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.StalhrimLight, EbonySmithing, DLC2OreStalhrim),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.NordicHeavy, AdvancedArmors, IngotCorundum, CraftingSmelter, IngotSteel),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.BoneMoldHeavy, AdvancedArmors, IngotIron),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Chitin, AdvancedArmors, IngotCorundum),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Silver, xMASMIMaterialGoldAndSilver, ingotSilver),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Gold, xMASMIMaterialGoldAndSilver, IngotGold),
            new BaseMaterialArmorDefinition(BaseMaterialArmor.Wood, default, Charcoal, CraftingSmelter, Firewood01)
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
            IFormLink<IConstructibleGetter>? relatedMeltdownProduct)
        {
            BaseMaterialArmor = baseMaterial;
            SmithingPerk = relatedSmithingPerk;
            MeltdownProduct = relatedMeltdownProduct;
            MeltdownCraftingStation = CraftingSmelter;
            MeltdownProduct = relatedMeltdownProduct;
        }
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
    
    public static class BaseMaterialArmorExtensions
    {
        public static BaseMaterialArmorDefinition GetDefinition(this BaseMaterialArmor armor)
        {
            return BaseMaterialArmorDefinition.ByEnum[armor];
        }
    
    }
    
}