using System;
using System.Collections.Generic;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout4;
using Newtonsoft.Json;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.DTOs.Armor
{
    public class ArmorMasqueradeBinding
    {
        [JsonProperty("faction")]
        public MasqueradeFaction Faction { get; set; } = MasqueradeFaction.None;

        [JsonProperty("substringArmors")]
        public string[] SubstringArmors { get; set; } = Array.Empty<string>();
    }
}