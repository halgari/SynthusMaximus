using Microsoft.Extensions.Logging;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthusMaximus.Data.Converters
{
    public class KeywordConverter : GenericFormLinkConverter<IKeywordGetter>
    {
        public KeywordConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ILogger<KeywordConverter> logger)
            : base(state.LoadOrder.PriorityOrder.Keyword().WinningOverrides(), logger)
        {
        }
    }
}