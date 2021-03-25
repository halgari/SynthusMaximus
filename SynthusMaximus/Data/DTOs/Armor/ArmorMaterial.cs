using System;
using Newtonsoft.Json;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.DTOs.Armor
{
    public class ArmorMaterial
    {
        [JsonProperty("class")]
        public ArmorClass Class { get; set; } = ArmorClass.Undefined;
        
        [JsonProperty("type")]
        public DynamicEnum<Material>.DynamicEnumMember Type { get; set; }
        
        [JsonProperty("armorBase")]
        public float ArmorBase { get; set; }
        
        [JsonProperty("nameSubstrings")] 
        public string[] SubStrings { get; set; } = Array.Empty<string>();

    }
}
