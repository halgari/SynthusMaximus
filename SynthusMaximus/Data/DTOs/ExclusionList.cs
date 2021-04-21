using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mutagen.Bethesda;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.DTOs
{
    public class ExclusionList<T> : MajorRecordExclusionList<IMajorRecordGetter>
    where T : ITranslatedNamedGetter
    {
        public ExclusionList(IDictionary<ExclusionType, List<Regex>> data) : base(data)
        {
        }
        
        public bool Matches(T record)
        {
            return List.Any(ex => CheckExclusion(ex.Key, ex.Value, record));
        }

        public override bool Matches(IMajorRecordGetter r)
        {
            return Matches((T) r) || base.Matches(r);
        }

        private bool CheckExclusion(ExclusionType ex, IReadOnlyCollection<Regex> patterns, ITranslatedNamedGetter a)
        {
            if (ex == ExclusionType.Name || ex == ExclusionType.Full)
            {
                if (a.Name == null || !a.Name!.TryLookup(Language.English, out var name))
                    return false;
                return CheckExclusionName(patterns, name);
            }

            return CheckExclusionMajorRecord(ex, patterns, (IMajorRecordGetter)a);
        }
        
        private bool CheckExclusionName(IEnumerable<Regex> patterns, string name)
        {
            return patterns.Any(p => p.IsMatch(name));
        }


    }
}