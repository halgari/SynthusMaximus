using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mutagen.Bethesda;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.DTOs
{
    public class MajorRecordExclusionList<T>
    where T : IMajorRecordGetter
    {
        protected readonly IDictionary<ExclusionType, List<Regex>> List;

        public MajorRecordExclusionList(IDictionary<ExclusionType, List<Regex>> data)
        {
            List = data;
        }

        public MajorRecordExclusionList()
        {
            List = new Dictionary<ExclusionType, List<Regex>>();
        }

        public virtual bool IsExcluded(T record)
        {
            return List.Any(ex => CheckExclusionMajorRecord(ex.Key, ex.Value, record));
        }

        protected bool CheckExclusionMajorRecord(ExclusionType e, IReadOnlyCollection<Regex> patterns, IMajorRecordGetter m)
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

        
    }
}