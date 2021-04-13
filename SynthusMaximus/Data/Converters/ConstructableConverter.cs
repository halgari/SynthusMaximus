using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthusMaximus.Data.Converters
{
    public class ConstructableConverter : GenericFormLinkConverter<IConstructibleGetter>
    {
        public ConstructableConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILogger<ConstructableConverter> logger) : 
            base(state.LoadOrder.PriorityOrder.IConstructible().WinningOverrides(), logger)
        {
        }
    }
}