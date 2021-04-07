using System.Collections.Generic;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthusMaximus.Data.Converters
{
    public class ObjectEffectConverter : GenericFormLinkConverter<IObjectEffectGetter>
    {
        public ObjectEffectConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : 
            base(state.LoadOrder.PriorityOrder.ObjectEffect().WinningOverrides())
        {
        }
    }
}