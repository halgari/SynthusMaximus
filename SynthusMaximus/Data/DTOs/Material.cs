using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs
{
    public class Material
    {
        [JsonProperty("smithingPerk")]
        public IFormLink<IPerkGetter>? SmithingPerk { get; set; } = new FormLink<IPerkGetter>();

        [JsonProperty("breakdownProduct")]
        public IFormLink<IItemGetter>? BreakdownProduct { get; set; } = new FormLink<IItemGetter>();

        [JsonProperty("breakdownStation")]
        public IFormLink<IKeywordGetter>? BreakdownStation { get; set; } = new FormLink<IKeywordGetter>();

        [JsonProperty("temperingInput")]
        public IFormLink<IItemGetter>? TemperingInput { get; set; } = new FormLink<IItemGetter>();
    }
}