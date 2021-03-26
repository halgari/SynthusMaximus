using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.Converters
{
    public class RegexConverter : JsonConverter<Regex>, IInjectedConverter
    {
        public override void WriteJson(JsonWriter writer, Regex value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override Regex ReadJson(JsonReader reader, Type objectType, Regex existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            return new Regex((string)reader.Value!);
        }
    }
}