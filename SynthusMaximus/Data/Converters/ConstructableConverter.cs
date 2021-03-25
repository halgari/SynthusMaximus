using System.Collections.Generic;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthusMaximus.Data.Converters
{
    public class ConstructableConverter : GenericFormLinkConverter<IConstructibleGetter>
    {
        public ConstructableConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : 
            base(state.LoadOrder.PriorityOrder.IConstructible().WinningOverrides())
        {
        }
    }
}