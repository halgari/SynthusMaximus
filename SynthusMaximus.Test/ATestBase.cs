using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Neon.Xunit;
using Noggog;
using Wabbajack.Common;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Update.Keyword;

namespace SynthusMaximus.Test
{
    public class ATestBase
    {
        protected LoadOrder<IModListing<SkyrimMod>> LEMods;
        protected LoadOrder<IModListing<SkyrimMod>> SEMods;
        public IModListing<SkyrimMod> OurMod;
        public IModListing<SkyrimMod> TheirMod;


        public ATestBase(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        public ITestOutputHelper OutputHelper { get; }

        protected void Setup()
        {
            LoadLELoadOrder();
            RunPatcher();
            LoadSELoadOrder();
        }

        protected void LoadLELoadOrder()
        {
            foreach (var file in 
                Game.Skyrim.MetaData().GameLocation().Combine("Data").EnumerateFiles(false, "*.esm"))
            {
                var toFile = AbsolutePath.EntryPoint.Combine("Resources", "LegendaryEdition").Combine(file.FileName);
                if (toFile.Exists && toFile.Size == file.Size)
                    continue;
                file.CopyToAsync(toFile)
                    .Wait();


            }
            
            foreach (var file in 
                Game.Skyrim.MetaData().GameLocation().Combine("Data").EnumerateFiles(false, "*.bsa"))
            {
                var toFile = AbsolutePath.EntryPoint.Combine("Resources", "LegendaryEdition").Combine(file.FileName);
                if (toFile.Exists && toFile.Size == file.Size)
                    continue;
                file.CopyToAsync(toFile)
                    .Wait();
            }


            
            LEMods = LoadOrder.Import<SkyrimMod>(new DirectoryPath(@".\Resources\LegendaryEdition"), new List<ModKey>()
            {
                ModKey.FromNameAndExtension("Skyrim.esm"),
                ModKey.FromNameAndExtension("Update.esm"),
                ModKey.FromNameAndExtension("Dawnguard.esm"),
                ModKey.FromNameAndExtension("Hearthfires.esm"),
                ModKey.FromNameAndExtension("Dragonborn.esm"),
                ModKey.FromNameAndExtension("PerkusMaximus_Master.esp"),
                ModKey.FromNameAndExtension("PerkusMaximus_Mage.esp"),
                ModKey.FromNameAndExtension("PerkusMaximus_Thief.esp"),
                ModKey.FromNameAndExtension("PerkusMaximus_Warrior.esp"),
                ModKey.FromNameAndExtension("PatchusMaximus.esp")
            }, GameRelease.SkyrimLE);
            TheirMod = LEMods.PriorityOrder.First();
        }



        protected void LoadSELoadOrder()
        {
            SEMods = LoadOrder.Import<SkyrimMod>(new DirectoryPath(@".\Resources\SpecialEdition"), new List<ModKey>()
            {
                ModKey.FromNameAndExtension("Skyrim.esm"),
                ModKey.FromNameAndExtension("Update.esm"),
                ModKey.FromNameAndExtension("Dawnguard.esm"),
                ModKey.FromNameAndExtension("Hearthfires.esm"),
                ModKey.FromNameAndExtension("Dragonborn.esm"),
                ModKey.FromNameAndExtension("PerkusMaximus_Master.esp"),
                ModKey.FromNameAndExtension("PerkusMaximus_Mage.esp"),
                ModKey.FromNameAndExtension("PerkusMaximus_Thief.esp"),
                ModKey.FromNameAndExtension("PerkusMaximus_Warrior.esp"),
                ModKey.FromNameAndExtension("SynthusMaximus.esp")
            }, GameRelease.SkyrimSE);

            OurMod = SEMods.PriorityOrder.First();
        }



        protected bool RunPatcher()
        {
            foreach (var file in 
                Game.SkyrimSpecialEdition.MetaData().GameLocation().Combine("Data").EnumerateFiles(false, "*.esm"))
            {
                var toFile = AbsolutePath.EntryPoint.Combine("Resources", "SpecialEdition").Combine(file.FileName);
                if (toFile.Exists && toFile.Size == file.Size)
                    continue;
                file.CopyToAsync(toFile)
                    .Wait();


            }
            
            foreach (var file in 
                Game.SkyrimSpecialEdition.MetaData().GameLocation().Combine("Data").EnumerateFiles(false, "*.bsa"))
            {
                var toFile = AbsolutePath.EntryPoint.Combine("Resources", "SpecialEdition").Combine(file.FileName);
                if (toFile.Exists && toFile.Size == file.Size)
                    continue;
                file.CopyToAsync(toFile)
                    .Wait();
            }


            var loadOrder = AbsolutePath.EntryPoint.Combine("Plugins.txt");
            loadOrder.WriteAllLinesAsync(
                "*Skyrim.esm",
                "*Update.esm",
                "*Dawnguard.esm",
                "*Hearthfires.esm",
                "*Dragonborn.esm",
                "*PerkusMaximus_Master.esp",
                "*PerkusMaximus_Mage.esp",
                "*PerkusMaximus_Thief.esp",
                "*PerkusMaximus_Warrior.esp").Wait();

            var stringWriter = new StringWriter();
            
            Console.SetOut(stringWriter);
            Console.SetError(stringWriter);
            Program.AddLogger = logging =>
            {
                logging.AddXUnit(OutputHelper);
            };
            
            var result = Program.Main(new string[]
            {
                "run-patcher",
                "--DataFolderPath", AbsolutePath.EntryPoint.Combine("Resources", "SpecialEdition").ToString(),
                "--GameRelease", "SkyrimSE",
                "--LoadOrderFilePath", loadOrder.ToString(),
                "--OutputPath", AbsolutePath.EntryPoint.Combine("Resources", "SpecialEdition", "SynthusMaximus.esp").ToString()
            }).Result;

            OutputHelper.WriteLine(stringWriter.ToString());
            return result == 0;
        }

        public void AssertEqual(IArmorGetter a, IArmorGetter b)
        {
            
            Assert.Equal(a.FormKey, b.FormKey);
            Assert.Equal(a.Name.NameOrEmpty(), b.Name.NameOrEmpty());
            Assert.True(Math.Abs((int)a.Value - (int)b.Value) <= 1); // Some rounding error is okay
            Assert.Equal(a.Weight, b.Weight, 4);
            Assert.Equal(a.BodyTemplate!.ArmorType, b.BodyTemplate!.ArmorType);
            AssertEqual(a.Keywords, b.Keywords);
        }

        private static FormLink<IKeywordGetter>[] SurvivalKeywords = {
            Survival_ArmorCold,
            Survival_ArmorWarm,
            Survival_BodyAndHead,
            Survival_LocTypeFreeShrineUse
        };
        
        private void AssertEqual(IReadOnlyList<IFormLinkGetter<IKeywordGetter>>? a, IReadOnlyList<IFormLinkGetter<IKeywordGetter>>? b)
        {
            a ??= new List<IFormLinkGetter<IKeywordGetter>>();
            b ??= new List<IFormLinkGetter<IKeywordGetter>>();
            var aset = a!.ToHashSet();
            var bset = b!.ToHashSet();
            aset.RemoveWhere(SurvivalKeywords.Contains);
            bset.RemoveWhere(SurvivalKeywords.Contains);
            Assert.Equal(aset, bset);
        }

        public bool AreEqual(IArmorGetter a, IArmorGetter b)
        {
            try
            {
                AssertEqual(a, b);
                return true;
            }
            catch (XunitException)
            {
                return false;
            }
        }
    }
}