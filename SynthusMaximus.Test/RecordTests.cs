using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Perk;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.MiscItem;
using static SynthusMaximus.Data.Statics;

namespace SynthusMaximus.Test
{
    public class RecordTests : ATestBase
    {
        public RecordTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            
        }

        [Fact]
        public void TestData()
        {
            Setup();

            var ourArmors = OurMod.Mod!.Armors.ToDictionary(a => a.FormKey);

            var butTop = LEMods.PriorityOrder.Skip(1).Armor().WinningOverrides().ToDictionary(a => a.FormKey);
            foreach (var theirArmor in TheirMod.Mod!.Armors)
            {
                try
                {
                    if (theirArmor.FormKey.ModKey.Name == "PatchusMaximus") continue;
                    if (theirArmor.NameOrEmpty().StartsWith(SReforged)) continue;
                    if (theirArmor.NameOrEmpty().StartsWith(SWarforged)) continue;
                    if (theirArmor.NameOrEmpty().EndsWith("[" + SReplica + "]")) continue;
                    if (theirArmor.NameOrEmpty().EndsWith("[" + SQuality + "]")) continue;

                    var weHave = ourArmors.ContainsKey(theirArmor.FormKey);
                    if (!weHave && butTop.TryGetValue(theirArmor.FormKey, out var vanillaArmor))
                    {
                        if (AreEqual(vanillaArmor, theirArmor))
                            continue;
                    }
                    Assert.True(weHave, $"We must have the armor");

                    var ourArmor = ourArmors[theirArmor.FormKey];

                    AssertEqual(theirArmor, ourArmor);
                }
                catch (XunitException)
                {
                    OutputHelper.WriteLine($"While comparing {theirArmor.EditorID} - {theirArmor.FormKey} - {theirArmor.Name.NameOrEmpty()}");
                    throw;
                }
            }

        }


    }
}