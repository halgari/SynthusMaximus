using Microsoft.Extensions.Logging;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthusMaximus.Data.Converters
{
    public class ItemConverter : GenericFormLinkConverter<IItemGetter>, ITryAfter<LeveledItemConverter>
    {
        public ItemConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILogger<ItemConverter> logger)
            : base(state.LoadOrder.PriorityOrder.IItem().WinningOverrides(), logger)
        {
        }
    }
}