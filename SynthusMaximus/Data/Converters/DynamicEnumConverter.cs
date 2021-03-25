using System;
using Newtonsoft.Json;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.Converters
{
    public class DynamicEnumConverter<TE, TM> : JsonConverter<DynamicEnum<TM>.DynamicEnumMember>
    where TE : DynamicEnum<TM>
    {
        private readonly TE _denum;

        public DynamicEnumConverter(TE denum)
        {
            _denum = denum;
        }


        public override void WriteJson(JsonWriter writer, DynamicEnum<TM>.DynamicEnumMember value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override DynamicEnum<TM>.DynamicEnumMember ReadJson(JsonReader reader, Type objectType, DynamicEnum<TM>.DynamicEnumMember existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var s = (string?) reader.Value;
            if (string.Equals(s, "NONE", StringComparison.InvariantCultureIgnoreCase))
                return default;
            return _denum![s!]!;
        }
    }
}