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
    public class TestData
    {
        private static TestData? _instance = null;
        public LoadOrder<IModListing<ISkyrimModGetter>> LEMods;
        public LoadOrder<IModListing<ISkyrimModGetter>> SEMods;
        public IModListing<ISkyrimModGetter> OurMod;
        public IModListing<ISkyrimModGetter> TheirMod;

        public ITestOutputHelper OutputHelper { get; }

        private static readonly object _lock = new();
        public static TestData Create(ITestOutputHelper output)
        {
            lock (_lock)
            {
                if (_instance != null) return _instance!;
                var d = new TestData();
                d.Setup(output);
                _instance = d;
                return _instance;
            }
        }
        private void Setup(ITestOutputHelper output)
        {
            LoadLELoadOrder();
            RunPatcher(output);
            LoadSELoadOrder();
        }

        private void LoadLELoadOrder()
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


            
            LEMods = LoadOrder.Import<ISkyrimModGetter>(new DirectoryPath(@".\Resources\LegendaryEdition"), new List<ModKey>()
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



        private void LoadSELoadOrder()
        {
            SEMods = LoadOrder.Import<ISkyrimModGetter>(new DirectoryPath(@".\Resources\SpecialEdition"), new List<ModKey>()
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



        private void RunPatcher(ITestOutputHelper helper)
        {
            var outputFile = AbsolutePath.EntryPoint.Combine("Resources", "SpecialEdition", "SynthusMaximus.esp");
            var sourceLastModified = ((AbsolutePath) typeof(Program).Assembly.Location).LastModifiedUtc;

            if (sourceLastModified < outputFile.LastModifiedUtc)
                return;
            
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

            if (helper != null)
            {
                Program.AddLogger = logging => { logging.AddXUnit(helper); };
            }

            var result = Program.Main(new string[]
            {
                "run-patcher",
                "--DataFolderPath", AbsolutePath.EntryPoint.Combine("Resources", "SpecialEdition").ToString(),
                "--GameRelease", "SkyrimSE",
                "--LoadOrderFilePath", loadOrder.ToString(),
                "--OutputPath", outputFile.ToString()
            }).Result;

            helper?.WriteLine(stringWriter.ToString());
            if (result != 0)
                throw new Exception("Patcher exception");
        }
    }

    public class MatchRecord<T>
    where T : IMajorRecordGetter
    {
        public MatchRecord(T expected, T actual)
        {
            Expected = expected;
            Actual = actual;
        }
        public override string ToString()
        {
            return $"{Expected.FormKey} = {Actual.FormKey}";
        }

        public T Expected { get; }
        public T Actual { get; }
    }
    
    public class ATestBase
    {

        private TestData _data;
        public LoadOrder<IModListing<ISkyrimModGetter>> LEMods => _data.LEMods;
        public LoadOrder<IModListing<ISkyrimModGetter>> SEMods => _data.SEMods;
        public IModListing<ISkyrimModGetter> OurMod => _data.OurMod;
        public IModListing<ISkyrimModGetter> TheirMod => _data.TheirMod;
        
        public ImmutableLoadOrderLinkCache<ISkyrimMod, ISkyrimModGetter> LELinkCache { get; }
        public ImmutableLoadOrderLinkCache<ISkyrimMod, ISkyrimModGetter> SELinkCache { get; }       

        public ATestBase(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
            _data = TestData.Create(outputHelper);
            LELinkCache = new ImmutableLoadOrderLinkCache<ISkyrimMod, ISkyrimModGetter>(LEMods.PriorityOrder.Select(m => m.Mod), LinkCachePreferences.Default);
            SELinkCache = new ImmutableLoadOrderLinkCache<ISkyrimMod, ISkyrimModGetter>(SEMods.PriorityOrder.Select(m => m.Mod), LinkCachePreferences.Default);
        }




        public ITestOutputHelper OutputHelper { get; set; }


        public static void AssertEqual(IArmorGetter a, IArmorGetter b, bool sameFormkey = false)
        {
            if (sameFormkey)
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


        private static void AssertEqual(IReadOnlyList<IFormLinkGetter<IKeywordGetter>>? a, IReadOnlyList<IFormLinkGetter<IKeywordGetter>>? b)
        {
            a ??= new List<IFormLinkGetter<IKeywordGetter>>();
            b ??= new List<IFormLinkGetter<IKeywordGetter>>();
            var aset = a!.ToHashSet();
            var bset = b!.ToHashSet();
            aset.RemoveWhere(SurvivalKeywords.Contains);
            bset.RemoveWhere(SurvivalKeywords.Contains);
            Assert.Equal(aset, bset);
        }

        public static bool AreEqual(IArmorGetter a, IArmorGetter b)
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