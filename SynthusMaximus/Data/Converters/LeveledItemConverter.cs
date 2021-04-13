using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthusMaximus.Data.Converters
{
    public class LeveledItemConverter : GenericFormLinkConverter<ILeveledItemGetter>
    {
        public LeveledItemConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILogger<LeveledItemConverter> logger) 
            : base(state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides(), logger)
        {
        }
    }
}