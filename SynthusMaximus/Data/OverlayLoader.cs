using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Newtonsoft.Json;
using SynthusMaximus.Data.DTOs;
using SynthusMaximus.Data.Enums;
using Wabbajack.Common;

namespace SynthusMaximus.Data
{
    public class OverlayLoader
    {
        private ILogger<OverlayLoader> _logger;
        private IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
        
        public List<AbsolutePath> Roots { get; }
        
        public JsonConverter[] Converters { get; set; }

        public OverlayLoader(ILogger<OverlayLoader> logger, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            _logger = logger;
            _state = state;
            Roots = new()
            {
                ((AbsolutePath) _state.DataFolderPath).Combine("config"),
                AbsolutePath.EntryPoint.Combine("config")
            };

        }

        private JsonSerializerSettings Settings => new() {Converters = Converters};

        /// <summary>
        /// Returns a list of all config files that match the given relative name, in order of the matching
        /// mods in the game load order.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<AbsolutePath> OverlayFiles(RelativePath name)
        {
            return _state.LoadOrder
                .SelectMany(modKey => Roots, (modKey, root) => root.Combine(modKey.Key.Name, name.ToString()))
                .Where(path => path.Exists)
                .Select(p =>
                {
                    _logger.LogInformation("Found : {Path} for {Name}", p, name);
                    return p;
                });
        }

        /// <summary>
        /// Merges all the values from all files with a given name, each key/value overwrites the previous
        /// with the same key. 
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="TK"></typeparam>
        /// <typeparam name="TV"></typeparam>
        /// <returns></returns>
        public IDictionary<TK, TV> LoadDictionary<TK, TV>(RelativePath name)
            where TK : notnull
        {
            return OverlayFiles(name)
                .Select(f => JsonConvert.DeserializeObject<Dictionary<TK, TV>>(f.ReadAllText(), Settings))
                .SelectMany(f => f)
                .GroupBy(f => f.Key)
                .ToDictionary(f => f.Key, f => f.Last().Value);
        }
        
        /// <summary>
        /// Merges all the values from all files with a given name, each key/value overwrites the previous
        /// with the same key. 
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="TK"></typeparam>
        /// <typeparam name="TV"></typeparam>
        /// <returns></returns>
        public IDictionary<string, TV> LoadDictionaryCaseInsensitive<TV>(RelativePath name)
        {
            return OverlayFiles(name)
                .Select(f => JsonConvert.DeserializeObject<Dictionary<string, TV>>(f.ReadAllText(), Settings)!)
                .SelectMany(f => f)
                .Aggregate(new Dictionary<string, TV>(StringComparer.InvariantCultureIgnoreCase),
                    (acc, kv) =>
                    {
                        acc[kv.Key] = kv.Value;
                        return acc;
                    });
        }

        /// <summary>
        /// Merges all the values from all the files with a given name, all the lists are concatenated together.
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IList<T> LoadList<T>(RelativePath name)
        {
            return OverlayFiles(name)
                .SelectMany(f => JsonConvert.DeserializeObject<List<T>>(f.ReadAllText(), Settings)!)
                .ToList();
        }

        public ExclusionList<T> LoadExclusionList<T>(RelativePath name)
            where T : ITranslatedNamedGetter
        {
            var data = LoadValueConcatDictionary<ExclusionType, Regex>(name);
            return new ExclusionList<T>(data);

        }
        
        public ComplexExclusionList<T> LoadComplexExclusionList<T>(RelativePath name)
            where T : IMajorRecordGetter, ITranslatedNamedGetter
        {
            var data = LoadList<ComplexExclusion>(name);
            return new ComplexExclusionList<T>(data);

        }
        
        public MajorRecordExclusionList<T> LoadMajorRecordExclusionList<T>(RelativePath name)
            where T : IMajorRecordGetter
        {
            var data = LoadValueConcatDictionary<ExclusionType, Regex>(name);
            return new MajorRecordExclusionList<T>(data);

        }

        /// <summary>
        /// Like LoadDictionary, except the values from each file are concatenated instead of replaced. 
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="TK"></typeparam>
        /// <typeparam name="TV"></typeparam>
        /// <returns></returns>
        public IDictionary<TK, List<TV>> LoadValueConcatDictionary<TK, TV>(RelativePath name)
            where TK : notnull
        {
            return OverlayFiles(name)
                .Select(f => JsonConvert.DeserializeObject<Dictionary<TK, List<TV>>>(f.ReadAllText(), Settings))
                .SelectMany(f => f)
                .GroupBy(f => f.Key)
                .ToDictionary(f => f.Key, 
                    f => f.SelectMany(v => v.Value).ToList());
        }
        
        /// <summary>
        /// Loads the last file in the override chain.
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T LoadObject<T>(RelativePath name)
        {
            var file = OverlayFiles(name)
                .Last();
            return JsonConvert.DeserializeObject<T>(file.ReadAllText(), Settings)!;
        }
        
    }
}