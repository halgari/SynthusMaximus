using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mutagen.Bethesda;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.DTOs
{
    public class ExclusionList<T>
    where T : ITranslatedNamedGetter
    {
        private readonly IDictionary<ExclusionType, List<Regex>> _list;

        public ExclusionList(IDictionary<ExclusionType, List<Regex>> data)
        {
            _list = data;
        }

        public bool IsExcluded(T record)
        {
            return _list.Any(ex => CheckExclusion(ex.Key, ex.Value, record));
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

        private bool CheckExclusionMajorRecord(ExclusionType e, IReadOnlyCollection<Regex> patterns, IMajorRecordGetter m)
        {
            var fis = m.FormKey.ID.ToString("X8");
            if (!patterns.Any())
                return false;
            
            return e switch
            {
                ExclusionType.Name => throw new NotImplementedException("Should have been handled elsewhere"),
                ExclusionType.EDID => m.EditorID != null && patterns.Any(p => p.IsMatch(m.EditorID!)),
                ExclusionType.Full => throw new NotImplementedException("Should have been handled elsewhere"),
                ExclusionType.FormID => patterns.Any(p => p.IsMatch(fis)),
                _ => throw new ArgumentOutOfRangeException(nameof(e), e, null)
            };
        }

        private bool CheckExclusionName(IEnumerable<Regex> patterns, string name)
        {
            return patterns.Any(p => p.IsMatch(name));
        }
    }
}