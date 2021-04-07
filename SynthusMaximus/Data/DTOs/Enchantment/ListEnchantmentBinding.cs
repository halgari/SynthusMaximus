using System.Collections.Generic;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Enchantment
{
    public class ListEnchantmentBinding
    {
        [JsonProperty("fillListWithSimilars")] public bool FillListWithSimilars { get; set; }
        [JsonProperty("eidList")] public IFormLink<ILeveledItemGetter> EdidList { get; set; } = new FormLink<ILeveledItemGetter>();
        [JsonProperty("enchantmentReplacers")] public List<EnchantmentReplacer> Replacers { get; set; } = new();

    }

    public class EnchantmentReplacer
    {
        [JsonProperty("edidBase")] public IFormLink<IObjectEffectGetter> EdidBase { get; set; } = new FormLink<IObjectEffectGetter>();
        [JsonProperty("edidNew")] public IFormLink<IObjectEffectGetter> EdidNew { get; set; } = new FormLinkNullable<IObjectEffectGetter>();
    }
}
