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
    public enum MasqueradeFaction
    {
        [EnumMember(Value = "BANDIT")]
        Bandit,
        [EnumMember(Value = "CULTIST")]
        Cultist,
        [EnumMember(Value = "DAWNGUARD")]
        Dawnguard,
        [EnumMember(Value = "FALMER")]
        Falmer,
        [EnumMember(Value = "FORSWORN")]
        Forsworn,
        [EnumMember(Value = "IMPERIAL")]
        Imperial,
        [EnumMember(Value = "STORMCLOAK")]
        Stormcloak,
        [EnumMember(Value = "THALMOR")]
        Thalmor,
        [EnumMember(Value = "VAMPIRE")]
        Vampire,
        [EnumMember(Value = "NONE")]
        None,
    }
    
    public class MasqueradeFactionsDefinition
    {
        public static Dictionary<MasqueradeFaction, MasqueradeFactionsDefinition> ByEnum { get; }

        public static List<MasqueradeFactionsDefinition> Registry { get; } = new()
        {
            new MasqueradeFactionsDefinition(MasqueradeFaction.Bandit, xMASPEMasqueradeBanditKeyword),
            new MasqueradeFactionsDefinition(MasqueradeFaction.Cultist, xMASPEMasqueradeCultistKeyword),
            new MasqueradeFactionsDefinition(MasqueradeFaction.Dawnguard, xMASPEMasqueradeDawnguardKeyword),
            new MasqueradeFactionsDefinition(MasqueradeFaction.Falmer, xMASPEMasqueradeFalmerKeyword),
            new MasqueradeFactionsDefinition(MasqueradeFaction.Forsworn, xMASPEMasqueradeForswornKeyword),
            new MasqueradeFactionsDefinition(MasqueradeFaction.Imperial, xMASPEMasqueradeImperialKeyword),
            new MasqueradeFactionsDefinition(MasqueradeFaction.Stormcloak, xMASPEMasqueradeStormcloakKeyword),
            new MasqueradeFactionsDefinition(MasqueradeFaction.Thalmor, xMASPEMasqueradeThalmorKeyword),
            new MasqueradeFactionsDefinition(MasqueradeFaction.Vampire, xMASPEMasqueradeVampireKeyword),
            new MasqueradeFactionsDefinition(MasqueradeFaction.None, null),

        };
        
        static MasqueradeFactionsDefinition()
        {
            ByEnum = Registry.ToDictionary(e => e.Faction);
            foreach (var itm in Enum.GetValues<MasqueradeFaction>())
            {
                if (!ByEnum.ContainsKey(itm))
                    throw new InvalidDataException($"Missing enum data for {itm}");
            }
        }
        
        public MasqueradeFaction Faction { get; }
        public IFormLink<IKeywordGetter>? Keyword { get; }


        public MasqueradeFactionsDefinition(
            MasqueradeFaction faction,
            IFormLink<IKeywordGetter>? keyword)
        {
            Faction = faction;
            Keyword = keyword;
        }

    }
    
    public static class MasqueradeFactionsExtensions
    {
        public static MasqueradeFactionsDefinition GetDefinition(this MasqueradeFaction armor)
        {
            return MasqueradeFactionsDefinition.ByEnum[armor];
        }
    
    }
}