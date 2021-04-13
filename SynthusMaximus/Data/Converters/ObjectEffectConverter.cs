using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthusMaximus.Data.Converters
{
    public class ObjectEffectConverter : GenericFormLinkConverter<IObjectEffectGetter>
    {
        public ObjectEffectConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILogger<ObjectEffectConverter> logger) : 
            base(state.LoadOrder.PriorityOrder.ObjectEffect().WinningOverrides(), logger)
        {
        }
    }
}