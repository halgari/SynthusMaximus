using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SynthusMaximus.Data.DTOs;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.Converters
{
    public class MaterialEnumConverter : DynamicEnumConverter<MaterialEnum, Material>, IInjectedConverter
    {
        public MaterialEnumConverter(MaterialEnum denum, IEnumerable<IFormLinkJsonConverter> converters) : base(denum)
        {
            denum.Loader.Converters = converters.Cast<JsonConverter>().ToArray();
        }
    }
}