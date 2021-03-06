// Autogenerated by https://github.com/Mutagen-Modding/Mutagen.Bethesda.FormKeys

using Mutagen.Bethesda.Skyrim;

namespace Mutagen.Bethesda.FormKeys.SkyrimSE
{
    public static partial class PerkusMaximus_Master
    {
        public static class PlacedObject
        {
            private static FormLink<IPlacedObjectGetter> Construct(uint id) => new FormLink<IPlacedObjectGetter>(ModKey.MakeFormKey(id));
            public static FormLink<IPlacedObjectGetter> SneakToolsClimbingCollisionBoxDUPLICATE002 => Construct(0xd0b5b);
            public static FormLink<IPlacedObjectGetter> SneakToolsShopTeleportToMarkerDUPLICATE002 => Construct(0xd0b61);
            public static FormLink<IPlacedObjectGetter> SneakToolsShopTeleportFromMarkerDUPLICATE002 => Construct(0xd0b62);
            public static FormLink<IPlacedObjectGetter> SneakToolsClimbingCollisionBox002 => Construct(0xd0d5e);
        }
    }
}
