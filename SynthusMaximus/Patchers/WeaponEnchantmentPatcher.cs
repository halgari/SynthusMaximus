using Microsoft.Extensions.Logging;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;

namespace SynthusMaximus.Patchers
{
    public class WeaponEnchantmentPatcher : APatcher<WeaponEnchantmentPatcher>, IRunAfter<WeaponPatcher>
    {
        public WeaponEnchantmentPatcher(ILogger<WeaponEnchantmentPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }
        /// <summary>
        ///  
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>

        public override void RunPatcher()
        {
            throw new System.NotImplementedException();
        }
    }
}