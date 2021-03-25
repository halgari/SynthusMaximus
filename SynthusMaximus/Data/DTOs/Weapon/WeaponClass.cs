using System;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Weapon
{
    public class WeaponClass
    {
        [JsonProperty("keywords")]
        public IFormLink<IKeywordGetter>[] Keywords { get; set; } = Array.Empty<IFormLink<IKeywordGetter>>();
    }
}