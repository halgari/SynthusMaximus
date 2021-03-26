using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;

namespace SynthusMaximus.Data.Enums
{
    public enum DamageTier : int
    {
        [EnumMember(Value = "Zero")]
        Zero,
        [EnumMember(Value = "ONE")]
        One,
        [EnumMember(Value = "TWO")]
        Two,
        [EnumMember(Value = "THREE")]
        Three
    }
    
    public class DamageTierDefinition
    {
        public static Dictionary<DamageTier, DamageTierDefinition> ByEnum { get; }

        public static List<DamageTierDefinition> Registry { get; } = new()
        {
            new DamageTierDefinition(DamageTier.Zero, null, null, null),
            new DamageTierDefinition(DamageTier.One, xMAWARBleedTier1, xMAWARDebuffTier1, xMAWARStaggerTier1),
            new DamageTierDefinition(DamageTier.Two, xMAWARBleedTier2, xMAWARDebuffTier2, xMAWARStaggerTier2),
            new DamageTierDefinition(DamageTier.Three, xMAWARBleedTier3, xMAWARDebuffTier3, xMAWARStaggerTier3)
        };
        
        static DamageTierDefinition()
        {
            ByEnum = Registry.ToDictionary(e => e.Tier);
            foreach (var itm in Enum.GetValues<DamageTier>())
            {
                if (!ByEnum.ContainsKey(itm))
                    throw new InvalidDataException($"Missing enum data for {itm}");
            }
        }
        
        public DamageTier Tier { get; set; }
        public IFormLink<IKeywordGetter>? BleedKeyword { get; }
        public IFormLink<IKeywordGetter>? DebuffKeyword { get; }
        public IFormLink<IKeywordGetter>? StaggerKeyword { get; }


        public DamageTierDefinition(
            DamageTier tier,
            IFormLink<IKeywordGetter>? bleed,
            IFormLink<IKeywordGetter>? debuff,
            IFormLink<IKeywordGetter>? stagger)
            { 
                Tier = tier;
            BleedKeyword = bleed;
            DebuffKeyword = debuff;
            StaggerKeyword = stagger;
        }

    }
    
    public static class DamageTierExtensions
    {
        public static DamageTierDefinition GetDefinition(this DamageTier tier)
        {
            return DamageTierDefinition.ByEnum[tier];
        }
    
    }
}