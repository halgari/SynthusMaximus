using System.Linq;
using System.Security.Policy;
using Loqui;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Xunit;
using Xunit.Sdk;

namespace SynthusMaximus.Test
{
    public static class Extensions
    {
        public static void EqualsKeywords(ExtendedList<IFormLinkGetter<IKeywordGetter>>? a,
            ExtendedList<IFormLinkGetter<IKeywordGetter>>? b)
        {
            Assert.True(a != null && b != null || a == null && b == null);
            var diff = a!.Except(b!).ToArray();
            Assert.Empty(diff);
        }
        
    }
}