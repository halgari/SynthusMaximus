using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.DTOs.Weapon
{
    public class BaseWeaponType
    {
        [JsonProperty("weapon")]
        public IFormLink<IKeywordGetter> Keyword { get; set; } = new FormLink<IKeywordGetter>();

        [JsonProperty("school")]
        public IFormLink<IKeywordGetter> School { get; set; } = new FormLink<IKeywordGetter>();
    }
}