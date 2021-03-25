using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.Converters
{
    public abstract class UppercaseEnumConverter<T> : JsonConverter<T>, IInjectedConverter
    where T: struct, Enum
    {
        private readonly Dictionary<string, T> _dict;

        protected UppercaseEnumConverter()
        {
            _dict = Enum.GetNames<T>().ToDictionary(val => val.ToUpper(), val => Enum.Parse<T>(val));
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