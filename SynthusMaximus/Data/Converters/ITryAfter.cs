using Newtonsoft.Json;

namespace SynthusMaximus.Data.Converters
{
    /// <summary>
    /// Use this converter after trying T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITryAfter<T>
    where T : JsonConverter
    {
        
    }
}