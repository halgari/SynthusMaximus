using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.Converters
{
    public abstract class EnumValueConverter<T> : JsonConverter<T>, IInjectedConverter
    where T : struct, Enum
    {
        private Dictionary<string, T> _dict;

        public EnumValueConverter()
        {
            Dictionary<string, T> acc = new();

            foreach (var val in Enum.GetNames<T>())
            {
                var memInfo = typeof(T).GetMember(val.ToString());
                var attr = memInfo[0].GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault();
                acc.Add(attr!.Value!, Enum.Parse<T>(val));
            }

            _dict = acc;
        }
        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var name = (string?)reader.Value;
            if (name != null && _dict.TryGetValue(name!, out var r))
                return r;
            
            throw new InvalidDataException($"Cannot find {typeof(T).Name} with name {name}");
        }
    }
}