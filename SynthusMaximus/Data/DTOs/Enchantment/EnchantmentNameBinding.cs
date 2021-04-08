using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Enchantment
{
    public class EnchantmentNameBinding
    {
        [JsonProperty("edid")]
        public IFormLink<IObjectEffectGetter> Enchantment { get; set; } = new FormLink<IObjectEffectGetter>();
        [JsonProperty("name")]
        public string NameTemplate { get; set; } = "";
    }
}