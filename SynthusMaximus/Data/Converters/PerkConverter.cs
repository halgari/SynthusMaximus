using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Newtonsoft.Json;

namespace SynthusMaximus.Data.Converters
{
    public class PerkConverter : GenericFormLinkConverter<IPerkGetter>
    {
        public PerkConverter(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
            : base(state.LoadOrder.PriorityOrder.Perk().WinningOverrides())
        {
        }
    }
}