using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthusMaximus.Data.Converters
{
    public class ItemConverter : GenericFormLinkConverter<IItemGetter>
    {
        public ItemConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
            : base(state.LoadOrder.PriorityOrder.IItem().WinningOverrides())
        {
        }
    }
}