using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SynthusMaximus.Data.DTOs;
using SynthusMaximus.Data.DTOs.Weapon;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.Converters
{
    public class BaseWeaponTypeConverter : DynamicEnumConverter<BaseWeaponTypeEnum, BaseWeaponType>, IInjectedConverter
    {
        public BaseWeaponTypeConverter(BaseWeaponTypeEnum denum, IEnumerable<IFormLinkJsonConverter> converters) : base(denum)
        {
            denum.Loader.Converters = converters.Cast<JsonConverter>().ToArray();
        }
    }
}