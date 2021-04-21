using System.Collections.Generic;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Enchantment
{
    public class ListEnchantmentBinding
    {
        [JsonProperty("fillListWithSimilars")] public bool FillListWithSimilars { get; set; }
        [JsonProperty("edidList")] public string EdidList { get; set; } = "";
        [JsonProperty("enchantmentReplacers")] public List<EnchantmentReplacer> Replacers { get; set; } = new();

    }

    public class EnchantmentReplacer
    {
        [JsonProperty("edidBase")] public string EdidBase { get; set; } = "";
        [JsonProperty("edidNew")] public string EdidNew { get; set; } = "";
        [JsonIgnore] public ModKey? BaseModKey { get; set; }
    }
}
