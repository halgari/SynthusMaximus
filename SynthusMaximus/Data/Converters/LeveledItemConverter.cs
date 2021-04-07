using System.Collections.Generic;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthusMaximus.Data.Converters
{
    public class LeveledItemConverter : GenericFormLinkConverter<ILeveledItemGetter>
    {
        public LeveledItemConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) 
            : base(state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides())
        {
        }
    }
}