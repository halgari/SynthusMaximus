// Autogenerated by https://github.com/Mutagen-Modding/Mutagen.Bethesda.FormKeys

using Mutagen.Bethesda.Skyrim;

namespace Mutagen.Bethesda.FormKeys.SkyrimSE
{
    public static partial class PerkusMaximus_Master
    {
        public static class Location
        {
            private static FormLink<ILocationGetter> Construct(uint id) => new FormLink<ILocationGetter>(ModKey.MakeFormKey(id));
            public static FormLink<ILocationGetter> xMASNEShopLocation => Construct(0x626c1);
        }
    }
}