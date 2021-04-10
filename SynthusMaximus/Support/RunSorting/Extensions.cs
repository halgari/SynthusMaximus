using System.Collections.Generic;
using System;
using System.Linq;
using DynamicData;
using Noggog;
using SynthusMaximus.Data.Converters;

namespace SynthusMaximus.Support.RunSorting
{
    public static class Extensions
    {
        public static IEnumerable<T> SortByRunOrder<T>(this IEnumerable<T> coll)
        {
            var result = new List<T>();
            var ending = coll.Where(c => c.GetType().GetCustomAttributes(typeof(RunAtEndAttribute), true).Any())
                .ToList();
            var remain = coll.Where(e => !ending.Contains(e)).ToList();

            while (remain.Count > 0)
            {
                for (var i = remain.Count - 1; i >= 0; i--)
                {
                    var c = remain[i];
                    if (!GetLoadAfters<T>(c.GetType()).Any())
                    {
                        result.Add(c);
                        remain.RemoveAt(i);
                        continue;
                    }

                    if (GetLoadAfters<T>(c.GetType()).All(t => result.Select(r => r.GetType()).Contains(t)))
                    {
                        result.Add(c);
                        remain.RemoveAt(i);
                        continue;
                    }
                }
                
            }
            result.AddRange(ending);
            return result;
        }
        
        private static IEnumerable<Type> GetLoadAfters<T>(Type t)
        {
            return t.GetCustomAttributes(typeof(RunAfterAttribute), true)
                .Select(t => ((RunAfterAttribute)t).RunAfter);
        }
    }
}