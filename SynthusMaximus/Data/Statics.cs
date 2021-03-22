using System.Collections.Generic;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;

namespace SynthusMaximus.Data
{
    public static class Statics
    {
        public static readonly HashSet<FormLink<IKeywordGetter>> JewelryKeywords = new()
        {
            VendorItemJewelry,
            JewelryExpensive,
            ClothingRing,
            ClothingNecklace,
            ClothingCirclet,
        };

        public static readonly HashSet<FormLink<IKeywordGetter>> ClothingKeywords = new()
        {
            ClothingBody,
            ClothingHands,
            ClothingFeet,
            ClothingHead,
            VendorItemClothing,
            ArmorClothing,
            ClothingPoor,
            ClothingRich,
        };

        public const string SPrefixPatcher = "PaMa_";
        public const string SPrefixMeltdown = "MELTDOWN_";
        public const string SPrefixCrafting = "CRAFT_";
        public const string SPrefixTemper = "TEMPER_";
        public const string SPrefixWeapon = "WEAP_";
        public const string SPrefixArmor = "ARMO_";
        public const string SPrefixAmmunition = "AMMO_";
        public const string SPrefixClothing = "CLOTH_";
        public const string SPrefixProjectile = "PROJ_";
        public const string SPrefixEnchantment = "ENCH_";
        public const string SPrefixMagiceffect = "MGEF_";
        public const string SPrefixStaff = "STAFF_";
        public const string SPrefixScroll = "SCRO_";
        public const string SPrefixBook = "BOOK_";
        public const string SPrefixLvli = "LVLI_";

        public const string SCrossbowRecurve = "Recurve";
        public const string SCrossbowArbalest = "Arbalest";
        public const string SCrossbowSilenced = "Silenced";
        public const string SCrossbowLightweight = "Lightweight";

        public const string SAmmoStrong = "Strong";
        public const string SAmmoStrongest = "Strongest";
        public const string SAmmoExplosive = "Explosive";
        public const string SAmmoTimebomb = "Timebomb";
        public const string SAmmoFrost = "Frost";
        public const string SAmmoFire = "Fire";
        public const string SAmmoShock = "Shock";
        public const string SAmmoBarbed = "Barbed";
        public const string SAmmoLightsource = "Lightsource";
        public const string SAmmoNoisemaker = "Noisemaker";
        public const string SAmmoPoison = "Poisoned";

        public const string SWeaponCrossbowDesc = "Ignores 50% armor.";

        public const string SWeaponCrossbowArbalestDesc =
            "Deals double damage against blocking enemies, but fires slower.";

        public const string SWeaponCrossbowSilencedDesc = "Deals increased sneak attack damage.";
        public const string SWeaponCrossbowLightweightDesc = "Has increased attack speed.";
        public const string SWeaponCrossbowRecurveDesc = "Deals additional damage.";

        public const string SWeaponRefinedDesc =
            "Deals more bonus damage to undead, and is easier to handle than regular silver weapons.";

        public const string SScriptApplyperk = "xMAAddPerkWhileEquipped";
        public const string SScriptApplyperkProperty = "p";

        public const string SScriptSilversword = "SilverSwordScript";
        public const string SScriptSilverswordProperty = "SilverPerk";

        public const string SScriptShoutexp = "xMATHIShoutExpScript";
        public const string SScriptShoutexpProperty0 = "xMATHIShoutExpBase";
        public const string SScriptShoutexpProperty1 = "playerref";
        public const string SScriptShoutexpProperty2 = "expFactor";

        public const string SAmmoExplosiveDesc = "Explodes upon impact, dealing 60 points of non-elemental damage.";

        public const string SAmmoTimebombDesc =
            "Explodes 3 seconds after being fired into a surface, dealing 150 points of non-elemental damage.";

        public const string SAmmoBarbedDesc =
            "Deals 6 points of bleeding damag per second over 8 seconds, and slows the target down by 20%.";

        public const string SAmmoFrostDesc = "Explodes upon impact, dealing 30 points of frost damage.";
        public const string SAmmoFireDesc = "Explodes upon impact, dealing 30 points of fire damage.";
        public const string SAmmoShockDesc = "Explodes upon impact, dealing 30 points of shock damage.";
        public const string SAmmoLightsourceDesc = "Emits light after being fired.";
        public const string SAmmoNoisemakerDesc = "Emits sound upon impact, distracting enemies.";

        public const string SAmmoHeavyweightDesc =
            "Has a 50% increased chance to stagger, and a 25% chance to strike the target down.";

        public const string SAmmoPoisonDesc =
            "Explodes upon impact, dealing 3 points of poison damage per second for 20 seconds.";

        public const string SScroll = "Scroll";
        public const string SStaff = "Staff";
        public const string SReplica = "Replica";
        public const string SQuality = "Quality";
        public const string SDuration = "Duration";
        public const string SSeconds = "seconds";
        public const string SDurReplace = "<dur>";
        public const string SReforged = "Reforged";
        public const string SWarforged = "Warforged";
        public const string SShortbow = "Shortbow";
        public const string SLongbow = "Longbow";
        public const string SEnchantmentDelimiter = "of";
        public const int ExpensiveClothingThreshold = 50;

        public const string SMaster = "PerkusMaximus_Master.esp";
        public static FormLink<IPerkGetter> PerkSmithingMeltdown = new(FormKey.Factory($"0a82a5:{SMaster}"));
        
        
	    public static FormLink<IKeywordGetter> CraftingScroll = new(FormKey.Factory($"10da22:{SMaster}"));

	public static FormLink<IKeywordGetter> WeapTypeArmingSword = new(FormKey.Factory($"098eef:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeBastardSword = new(FormKey.Factory($"01006f:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeBattleaxePerMa = new(FormKey.Factory($"0d5e61:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeBattlestaff = new(FormKey.Factory($"010071:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeClub = new(FormKey.Factory($"010079:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeDaggerPerMa = new(FormKey.Factory($"27a600:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypePartisan = new(FormKey.Factory($"01006c:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeGreatswordPerMa = new(FormKey.Factory($"0d5e63:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeHalberd = new(FormKey.Factory($"01006d:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeKatana = new(FormKey.Factory($"010076:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeLongmace = new(FormKey.Factory($"010070:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeLongsword = new(FormKey.Factory($"010075:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeNodachi = new(FormKey.Factory($"01006e:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeScimitar = new(FormKey.Factory($"2703f4:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeSaber = new(FormKey.Factory($"2703f5:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeShortspear = new(FormKey.Factory($"010078:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeShortsword = new(FormKey.Factory($"010074:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeTanto = new(FormKey.Factory($"01007a:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeWakizashi = new(FormKey.Factory($"010077:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeMacePerMa = new(FormKey.Factory($"0d5e62:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeMaul = new(FormKey.Factory($"2703f6:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeHatchet = new(FormKey.Factory($"2703f7:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeWarhammerPerMa = new(FormKey.Factory($"0d5e60:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeWaraxePerMa = new(FormKey.Factory($"0d5e5f:{SMaster}"));
	// TODO remove
	public static FormLink<IKeywordGetter> WeapTypeFist = new(FormKey.Factory($"368770:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeClaw = new(FormKey.Factory($"558d87:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeKatar = new(FormKey.Factory($"558d89:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeKnuckles = new(FormKey.Factory($"558d88:{SMaster}"));

	public static FormLink<IKeywordGetter> WeapTypeCrossbow = new(FormKey.Factory($"010073:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeLongbow = new(FormKey.Factory($"010072:{SMaster}"));
	public static FormLink<IKeywordGetter> WeapTypeShortbow = new(FormKey.Factory($"098ef0:{SMaster}"));

	// crossbow variants

	public static FormLink<IKeywordGetter> CrossbowArbalest = new(FormKey.Factory($"40588a:{SMaster}"));
	public static FormLink<IKeywordGetter> CrossbowRecurve = new(FormKey.Factory($"40588b:{SMaster}"));
	public static FormLink<IKeywordGetter> CrossbowSilenced = new(FormKey.Factory($"405889:{SMaster}"));
	public static FormLink<IKeywordGetter> CrossbowLightweight = new(FormKey.Factory($"438308:{SMaster}"));

	// armor slots

	public static FormLink<IKeywordGetter> ArmorLightShield = new(FormKey.Factory($"2703f2:{SMaster}"));
	public static FormLink<IKeywordGetter> ArmorLightHead = new(FormKey.Factory($"0a3180:{SMaster}"));
	public static FormLink<IKeywordGetter> ArmorLightArms = new(FormKey.Factory($"0a317a:{SMaster}"));
	public static FormLink<IKeywordGetter> ArmorLightChest = new(FormKey.Factory($"0a317c:{SMaster}"));
	public static FormLink<IKeywordGetter> ArmorLightLegs = new(FormKey.Factory($"0a317d:{SMaster}"));

	public static FormLink<IKeywordGetter> ArmorHeavyShield = new(FormKey.Factory($"0a317e:{SMaster}"));
	public static FormLink<IKeywordGetter> ArmorHeavyHead = new(FormKey.Factory($"0a317f:{SMaster}"));
	public static FormLink<IKeywordGetter> ArmorHeavyArms = new(FormKey.Factory($"0a3179:{SMaster}"));
	public static FormLink<IKeywordGetter> ArmorHeavyChest = new(FormKey.Factory($"0a317b:{SMaster}"));
	public static FormLink<IKeywordGetter> ArmorHeavyLegs = new(FormKey.Factory($"2703f3:{SMaster}"));

	// weapon class

	public static FormLink<IKeywordGetter> WeaponClassBlade = new(FormKey.Factory($"0d5e65:{SMaster}"));
	public static FormLink<IKeywordGetter> WeaponClassBlunt = new(FormKey.Factory($"0d5e64:{SMaster}"));
	public static FormLink<IKeywordGetter> WeaponClassPiercing = new(FormKey.Factory($"1e7769:{SMaster}"));

	// weapon school

	public static FormLink<IKeywordGetter> WeaponSchoolLightWeaponry = new(FormKey.Factory($"2b222c:{SMaster}"));
	public static FormLink<IKeywordGetter> WeaponSchoolHeavyWeaponry = new(FormKey.Factory($"2b222d:{SMaster}"));
	public static FormLink<IKeywordGetter> WeaponSchoolRangedWeaponry = new(FormKey.Factory($"2b222e:{SMaster}"));

	// stagger, bleed, debuff

	public static FormLink<IKeywordGetter> WeaponStaggerTier1 = new(FormKey.Factory($"1599f1:{SMaster}"));
	public static FormLink<IKeywordGetter> WeaponStaggerTier2 = new(FormKey.Factory($"1599f2:{SMaster}"));
	public static FormLink<IKeywordGetter> WeaponStaggerTier3 = new(FormKey.Factory($"1599f3:{SMaster}"));

	public static FormLink<IKeywordGetter> WeaponDebuffTier1 = new(FormKey.Factory($"1599f5:{SMaster}"));
	public static FormLink<IKeywordGetter> WeaponDebuffTier2 = new(FormKey.Factory($"1599f6:{SMaster}"));
	public static FormLink<IKeywordGetter> WeaponDebuffTier3 = new(FormKey.Factory($"1599f7:{SMaster}"));

	public static FormLink<IKeywordGetter> WeaponBleedTier1 = new(FormKey.Factory($"1599eb:{SMaster}"));
	public static FormLink<IKeywordGetter> WeaponBleedTier2 = new(FormKey.Factory($"1599ec:{SMaster}"));
	public static FormLink<IKeywordGetter> WeaponBleedTier3 = new(FormKey.Factory($"1599ed:{SMaster}"));

	// masquerade

	public static FormLink<IKeywordGetter> MasqueradeForsworn = new(FormKey.Factory($"3125ae:{SMaster}"));
	public static FormLink<IKeywordGetter> MasqueradeThalmor = new(FormKey.Factory($"3125b0:{SMaster}"));
	public static FormLink<IKeywordGetter> MasqueradeBandit = new(FormKey.Factory($"3f143a:{SMaster}"));
	public static FormLink<IKeywordGetter> MasqueradeImperial = new(FormKey.Factory($"3125af:{SMaster}"));
	public static FormLink<IKeywordGetter> MasqueradeStormcloak = new(FormKey.Factory($"3125b2:{SMaster}"));

	public static FormLink<IKeywordGetter> MasqueradeCultist = new(FormKey.Factory($"3125b5:{SMaster}"));
	public static FormLink<IKeywordGetter> MasqueradeDawnguard = new(FormKey.Factory($"3125b4:{SMaster}"));
	public static FormLink<IKeywordGetter> MasqueradeFalmer = new(FormKey.Factory($"3125b1:{SMaster}"));
	public static FormLink<IKeywordGetter> MasqueradeVampire = new(FormKey.Factory($"3125b3:{SMaster}"));

	// random

	public static FormLink<IKeywordGetter> ScrollSpell = new(FormKey.Factory($"28b047:{SMaster}"));


	public static FormLink<IKeywordGetter> ShoutHarmful = new(FormKey.Factory($"2a8008:{SMaster}"));
	public static FormLink<IKeywordGetter> ShoutNonHarmful = new(FormKey.Factory($"3125ad:{SMaster}"));
	public static FormLink<IKeywordGetter> ShoutSummoning = new(FormKey.Factory($"2a800b:{SMaster}"));

	public static FormLink<IKeywordGetter> MagicDisarm = new(FormKey.Factory($"3960f8:{SMaster}"));

    }
}