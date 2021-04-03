using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Loqui;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Xunit;
using Xunit.Sdk;
using static SynthusMaximus.Data.Statics;

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

        public static IEnumerable<(IArmorGetter VanillaArmor, IConstructibleObjectGetter Recipe, IArmorGetter Reforged)>
            ReforgedArmor(this LoadOrder<IModListing<ISkyrimModGetter>> mods, IArmorGetter vanillaArmor)
        {
            return from cobj in mods.PriorityOrder.ConstructibleObject().WinningOverrides()
                where cobj.Items!.Any(rf => rf.Item.Item.FormKey == vanillaArmor.FormKey)
                from produces in mods.PriorityOrder.Armor().WinningOverrides()
                where cobj.CreatedObject.FormKey == produces.FormKey
                where produces.EditorID!.Contains($"[{SReplica}]")
                select (VanillaArmor: vanillaArmor, Recipe: cobj, Reforged: produces);
        }
        
    }
}