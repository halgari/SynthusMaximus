using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthusMaximus.Data.Converters
{
    public class KeywordConverter : GenericFormLinkConverter<IKeywordGetter>
    {
        public KeywordConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
            : base(state.LoadOrder.PriorityOrder.Keyword().WinningOverrides())
        {
        }
    }
}