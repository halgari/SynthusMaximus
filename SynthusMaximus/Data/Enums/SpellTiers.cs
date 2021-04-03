using System;
using System.Collections.Generic;
using System.Linq;

namespace SynthusMaximus.Data.Enums
{
    public enum SpellTier : int
    {
        Invalid = -1,
        Novice = 0,
        Apprentice = 25,
        Adept = 50,
        Expert = 75,
        Master = 100
    }

    public static class SpellTiers
    {
        private static readonly Dictionary<int, SpellTier> _indexed = Enum.GetValues<SpellTier>()
            .Select(s => (s, (int) s))
            .ToDictionary(s => s.Item2, s => s.s);
        
        public static SpellTier? FromLevel(int i)
        {
            return _indexed[i];
        }
    }
}