using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LoggingAdvanced.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Synthesis.Bethesda;
using SynthusMaximus.Data;
using SynthusMaximus.Patchers;
using Wabbajack.Common.StatusFeed.Errors;
using Armor = SynthusMaximus.Data.LowLevel.Armor;
using Microsoft.Extensions.Hosting;
using Wabbajack.Common;

namespace SynthusMaximus
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch).Run(args, new RunPreferences()
            {
                ActionsForEmptyArgs = new RunDefaultPatcher()
                {
                    IdentifyingModKey = "SynthusMaximus.esp",
                    TargetRelease = GameRelease.SkyrimSE,
                }
            });
        }


        public static async Task RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var host = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureAppConfiguration((hostingContext, configuration) =>
                {
                    configuration.Sources.Clear();
                    var env = hostingContext.HostingEnvironment;
                    configuration.AddJsonFile(AbsolutePath.EntryPoint.Combine("appsettings.json").ToString(), optional: false, reloadOnChange: false);
                })
                .ConfigureServices(c => ConfigureServices(c, state))
                .Build();
            var patcher = host.Services.GetService<ArmorPatcher>();
            if (patcher == null)
            {
                Console.WriteLine("Could not create service!");
                throw new InvalidOperationException("Could not create patcher");
            }

            patcher!.RunChanges();
        }

        private static void ConfigureServices(IServiceCollection collection, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            collection.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.ClearProviders();
                logging.AddConsole();
            });
            collection.AddSingleton<DataStorage>();
            collection.AddTransient<ArmorPatcher>();
            collection.AddSingleton(state);
        }
    }
}