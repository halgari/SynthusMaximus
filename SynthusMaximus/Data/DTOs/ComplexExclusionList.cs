using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mutagen.Bethesda;
using Noggog;
using SynthusMaximus.Data.Enums;
using System.Linq;

namespace SynthusMaximus.Data.DTOs
{
    public class ComplexExclusionList<T>
    where T : ITranslatedNamedGetter, IMajorRecordGetter
    {
        private IList<ComplexExclusion> _exclusions;

        public ComplexExclusionList(IList<ComplexExclusion> exclusions)
        {
            _exclusions = exclusions;
        }
        
        private bool CheckComplexExclusions(ComplexExclusion ex, T a, T b)
        {
            return CheckExclusion(ex.TargetA, new[] {ex.TextA}, a) && CheckExclusion(ex.TargetB, new[] {ex.TextB}, b) ||
                   CheckExclusion(ex.TargetB, new[] {ex.TextB}, a) && CheckExclusion(ex.TargetA, new[] {ex.TextA}, b);
        }
        
        public bool IsExcluded(T a, T b)
        {
            return _exclusions.Any(ex => CheckComplexExclusions(ex, a, b));
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
                ExclusionType.ModName => patterns.Any(p => p.IsMatch(m.FormKey.ModKey.FileName)),
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