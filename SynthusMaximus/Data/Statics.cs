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
        
    }
}