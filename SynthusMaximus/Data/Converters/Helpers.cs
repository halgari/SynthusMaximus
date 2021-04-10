using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Noggog;

namespace SynthusMaximus.Data.Converters
{
    public static class Helpers
    {
        public static List<JsonConverter> SortConverters(this IEnumerable<JsonConverter> converters)
        {
            var result = new List<JsonConverter>();
            var remain = converters.ToList();
            while (remain.Count > 0)
            {
                for (var i = remain.Count - 1; i >= 0; i--)
                {
                    var c = remain[i];
                    if (!GetLoadAfters(c.GetType()).Any())
                    {
                        result.Add(c);
                        remain.RemoveAt(i);
                        continue;
                    }

                    if (GetLoadAfters(c.GetType()).All(t => result.Select(r => r.GetType()).Contains(t)))
                    {
                        result.Add(c);
                        remain.RemoveAt(i);
                        continue;
                    }
                }
                
            }

            return result;
        }
        
        private static IEnumerable<Type> GetLoadAfters(Type t)
        {
            return t.GetInterfaces()
                .Where(e => e.IsGenericType)
                .Where(e => e.GetGenericTypeDefinition() == typeof(ITryAfter<>))
                .SelectMany(e => e.GetGenericArguments());
        }
    }
}