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
    public enum BaseMaterialWeapon
    {
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
        [EnumMember(Value = "ELVEN")] Elven,
        [EnumMember(Value = "SCALED")] Scaled,
        [EnumMember(Value = "GLASS")] Glass,
        [EnumMember(Value = "DRAGONSCALE")] DragonScale,
        [EnumMember(Value = "STALHRIM")] Stalhrim,
        [EnumMember(Value = "WOOD")] Wood,
        [EnumMember(Value = "ADVANCED")] Advanced,
        [EnumMember(Value = "SILVER")] Silver,
        [EnumMember(Value = "REFINED_SILVER")] RefinedSilver,
        [EnumMember(Value = "DRAUGR")] Draugr,
        [EnumMember(Value = "CHITIN")] Chitin,
        [EnumMember(Value = "GOLD")] Gold,
        [EnumMember(Value = "BONEMOLD_HEAVY")] BonemoldHeavy
    };

    public class BaseMaterialWeaponDefinition
    {
        public static Dictionary<BaseMaterialWeapon, BaseMaterialWeaponDefinition> ByEnum { get; }

        public static List<BaseMaterialWeaponDefinition> Registry { get; } = new()
        {
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.None, default, default, default, default),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Iron, default, IngotIron),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Steel, SteelSmithing, IngotSteel),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Dwarven, DwarvenSmithing, IngotDwarven),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Falmer, AdvancedArmors, ChaurusChitin),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Orcish, OrcishSmithing, IngotOrichalcum),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.SteelPlate, AdvancedArmors, IngotSteel),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Ebony, EbonySmithing, IngotEbony),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.DragonPlate, DragonArmor, DragonBone),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Daedric, DaedricSmithing, IngotEbony),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Elven, ElvenSmithing, IngotIMoonstone),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Scaled, AdvancedArmors, IngotCorundum),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Glass, GlassSmithing, IngotMalachite),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Stalhrim, EbonySmithing, DLC2OreStalhrim),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Wood, null, Charcoal, CraftingSmelter, Firewood01),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Advanced, AdvancedArmors, IngotCorundum, CraftingSmelter, IngotCorundum),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Silver, xMASMIMaterialGoldAndSilver, ingotSilver),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.RefinedSilver, xMARefinedSilverPerk, ingotSilver),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Draugr, SteelSmithing, IngotSteel),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Chitin, AdvancedArmors, IngotCorundum),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.Gold, xMASMIMaterialGoldAndSilver, IngotGold),
            new BaseMaterialWeaponDefinition(BaseMaterialWeapon.BonemoldHeavy, AdvancedArmors, IngotIron)
        };
        
        static BaseMaterialWeaponDefinition()
        {
            ByEnum = Registry.ToDictionary(e => e.BaseMaterialWeapon);
            foreach (var itm in Enum.GetValues<BaseMaterialWeapon>())
            {
                if (!ByEnum.ContainsKey(itm))
                    throw new InvalidDataException($"Missing enum data for {itm}");
            }
        }

        public BaseMaterialWeapon BaseMaterialWeapon { get; }
        public IFormLink<IPerkGetter>? SmithingPerk { get; }
        public IFormLink<IConstructibleGetter>? MeltdownProduct { get; }
        public IFormLink<IKeywordGetter>? MeltdownCraftingStation { get; }
        public IFormLink<IItemGetter>? TemperingInput { get; }

        public BaseMaterialWeaponDefinition(
            BaseMaterialWeapon baseMaterial,
            IFormLink<IPerkGetter>? relatedSmithingPerk,
            IFormLink<IConstructibleGetter>? relatedMeltdownProduct)
        {
            BaseMaterialWeapon = baseMaterial;
            SmithingPerk = relatedSmithingPerk;
            MeltdownProduct = relatedMeltdownProduct;
            MeltdownCraftingStation = CraftingSmelter;
            MeltdownProduct = relatedMeltdownProduct;
        }
        public BaseMaterialWeaponDefinition(
            BaseMaterialWeapon baseMaterial,
            IFormLink<IPerkGetter>? relatedSmithingPerk,
            IFormLink<IConstructibleGetter>? relatedMeltdownProduct,
            IFormLink<IKeywordGetter>? relatedMeltdownCraftingStation, 
            IFormLink<IItemGetter>? relatedTemperingInput)
        {
            BaseMaterialWeapon = baseMaterial;
            SmithingPerk = relatedSmithingPerk;
            MeltdownProduct = relatedMeltdownProduct;
            MeltdownCraftingStation = relatedMeltdownCraftingStation;
            TemperingInput = relatedTemperingInput;
        }
    }
    
    public static class BaseMaterialWeaponExtensions
    {
        public static BaseMaterialWeaponDefinition GetDefinition(this BaseMaterialWeapon armor)
        {
            return BaseMaterialWeaponDefinition.ByEnum[armor];
        }
    
    }
    
}