using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Enchantment
{
    public class DirectEnchantmentBinding
    {
        [JsonProperty("base")]
        public IFormLink<IObjectEffectGetter> Base { get; set; } = new FormLink<IObjectEffectGetter>();
        [JsonProperty("new")]
        public IFormLink<IObjectEffectGetter> New { get; set; } = new FormLink<IObjectEffectGetter>();
    }
}