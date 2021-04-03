using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class ArmorTests : ATestBase
    {
        public ArmorTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            
        }
        
        [Fact]
        public void VanillaArmorsMatch()
        {
            var ourArmors = OurMod.Mod!.Armors.ToDictionary(a => a.FormKey);

            var butTop = LEMods.PriorityOrder.Skip(1).Armor().WinningOverrides().ToDictionary(a => a.FormKey);

            foreach (var theirArmor in TheirMod.Mod!.Armors)
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
                
                var ourArmor = ourArmors[theirArmor.FormKey];
                AssertEqual(theirArmor, ourArmor);
            }
        }
        
        [Fact]
        public void ReforgedArmorsMatch()
        {
            var ourArmors = SEMods.PriorityOrder.Armor().WinningOverrides().GroupBy(a => a.EditorID)
                .ToDictionary(a => a.Key);
            
            foreach (var theirArmor in TheirMod.Mod!.Armors.Where(t => t.NameOrEmpty().Contains(SReforged)))
            {
                var ourArmorsMatching = ourArmors[theirArmor.EditorID];

                foreach (var ourArmor in ourArmorsMatching)
                {
                    try
                    {

                        AssertEqual(theirArmor, ourArmor);
                    }
                    catch (XunitException)
                    {
                        OutputHelper.WriteLine($"{theirArmor.EditorID} {theirArmor.Name} {theirArmor.FormKey} | {ourArmor.EditorID} {ourArmor.Name} {ourArmor.FormKey}");
                        throw;
                    }
                }
            }

        }
    }
}