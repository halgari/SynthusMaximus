// Autogenerated by https://github.com/Mutagen-Modding/Mutagen.Bethesda.FormKeys

using Mutagen.Bethesda.Skyrim;

namespace Mutagen.Bethesda.FormKeys.SkyrimSE
{
    public static partial class PerkusMaximus_Master
    {
        public static class Outfit
        {
            private static FormLink<IOutfitGetter> Construct(uint id) => new FormLink<IOutfitGetter>(ModKey.MakeFormKey(id));
            public static FormLink<IOutfitGetter> xMASNEHorstarOutfit => Construct(0x63f92);
            public static FormLink<IOutfitGetter> xNPC_LeveledActorOutfitLIGHT => Construct(0x2e158);
            public static FormLink<IOutfitGetter> xNPC_LeveledActorOutfitMAGE => Construct(0x33263);
            public static FormLink<IOutfitGetter> xMASPELeadershipActorTankTeddyOutfit => Construct(0xd25e74);
            public static FormLink<IOutfitGetter> xNPC_LeveledActorOutfitHEAVY => Construct(0x2e157);
        }
    }
}