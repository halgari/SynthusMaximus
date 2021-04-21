using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using SynthusMaximus.Data.DTOs;
using SynthusMaximus.Support;
using SynthusMaximus.Support.RunSorting;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;
using static SynthusMaximus.Data.Statics;

namespace SynthusMaximus.Patchers
{
    [RunAfter(typeof(ArmorPatcher))]
    public class FillArmorListsWithSimilars : AFillWithSimilars<FillArmorListsWithSimilars, IArmorGetter>
    {
        public FillArmorListsWithSimilars(ILogger<FillArmorListsWithSimilars> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }
        
        protected override IArmorGetter CreateItemFromTemplate(IArmorGetter template, IArmorGetter like, IFormLink<IObjectEffectGetter> e)
        {

            var resolved = e.Resolve(State.LinkCache);
            var newArmor = Patch.Armors.DuplicateInAsNewRecord(template);
            newArmor.SetEditorID(
                SPrefixPatcher + SPrefixArmor + template.NameOrEmpty() + resolved.NameOrEmpty(), resolved);
            newArmor.TemplateArmor.SetTo(template);
            newArmor.ObjectEffect.SetTo(e);
            newArmor.Value = like.Value;
            newArmor.Name = Storage.GetLocalizedEnchantmentName(template, e);
            return newArmor;
        }

        protected override ExclusionList<IArmorGetter> GetEnchantmentExclusionList()
        {
            return Storage.EnchantmentArmorExclusions;
        }

        protected override IFormLinkNullableGetter<IEffectRecordGetter> GetEnchantment(IArmorGetter i)
        {
            return i.ObjectEffect;
        }

        protected override IFormLinkNullableGetter<IArmorGetter> GetTemplate(IArmorGetter i)
        {
            return i.TemplateArmor;
        }

        public override IEnumerable<IArmorGetter> GetSimilars(IArmorGetter a)
        {
            if (Storage.IsClothing(a))
            {
                foreach (var ae in Mods.Armor().WinningOverrides())
                {
                    if (ae.FormKey == a.FormKey) continue;
                    if (Storage.IsClothing(ae) && 
                        ae.ObjectEffect.IsNull &&
                        AreClothingPiecesSimilar(a, ae))
                    {
                        if (Storage.EnchantingSimilarityExclusionsArmor.IsExcluded(a, ae)) continue;
                        yield return ae;
                    }
                }
            }
            else if (Storage.IsJewelry(a))
            {
                foreach (var ae in Mods.Armor().WinningOverrides())
                {
                    if (ae.FormKey == a.FormKey) continue;
                    if (Storage.IsJewelry(ae) &&
                        ae.ObjectEffect.IsNull &&
                        AreJewelryPiecesSimilar(a, ae))
                    {
                        if (Storage.EnchantingSimilarityExclusionsArmor.IsExcluded(a, ae)) continue;
                        yield return ae;

                    }
                }
            }
            else
            {
                foreach (var ae in Mods.Armor().WinningOverrides())
                {
                    if (ae.FormKey == a.FormKey) continue;
                    if (!Storage.IsClothing(ae) &&
                        !Storage.IsJewelry(ae) &&
                        ae.ObjectEffect.IsNull &&
                        AreArmorPiecesSimilar(a, ae))
                    {
                        if (Storage.EnchantingSimilarityExclusionsArmor.IsExcluded(a, ae)) continue;
                        yield return ae;
                    }
                }
            }
        }

        private bool AreArmorPiecesSimilar(IArmorGetter a, IArmorGetter b)
        {
            var ama = Storage.GetArmorMaterial(a);
            var amb = Storage.GetArmorMaterial(b);
            if (ama == null || amb == null) return false;

            return DoClothingPicesHaveSameSlot(a, b) &&
                   DoArmorPiecesHaveSameType(a, b) &&
                ama.Type.Data?.BreakdownProduct?.FormKey == amb.Type.Data?.BreakdownProduct?.FormKey &&
                ama.Type.Data?.TemperingInput?.FormKey == amb.Type.Data?.TemperingInput?.FormKey;
        }

        private bool DoArmorPiecesHaveSameType(IArmorGetter a, IArmorGetter b)
        {
            return a.HasKeyword(ArmorHeavy) && b.HasKeyword(ArmorHeavy) ||
                   a.HasKeyword(ArmorLight) && b.HasKeyword(ArmorHeavy) ||
                   !a.HasKeyword(ArmorLight) && !a.HasKeyword(ArmorHeavy) &&
                   !b.HasKeyword(ArmorLight) && !b.HasKeyword(ArmorHeavy);
        }

        private bool AreJewelryPiecesSimilar(IArmorGetter a, IArmorGetter b)
        {
            return DoClothingPicesHaveSameSlot(a, b) && DoJewelryPiecesHaveSimilarPriceCategory(a, b);
        }

        private bool DoJewelryPiecesHaveSimilarPriceCategory(IArmorGetter a, IArmorGetter b)
        {
            return a.HasKeyword(JewelryExpensive) && b.HasKeyword(JewelryExpensive) ||
                   !a.HasKeyword(JewelryExpensive) && !b.HasKeyword(JewelryExpensive);
        }

        private bool AreClothingPiecesSimilar(IArmorGetter a, IArmorGetter b)
        {
            return DoClothingPicesHaveSameSlot(a, b) && DoClothingPiecesHaveSimilarPriceCategory(a, b);
        }

        private bool DoClothingPiecesHaveSimilarPriceCategory(IArmorGetter a, IArmorGetter b)
        {
            return a.HasKeyword(ClothingPoor) && b.HasKeyword(ClothingPoor) ||
                   a.HasKeyword(ClothingRich) && b.HasKeyword(ClothingRich) ||
                   !a.HasKeyword(ClothingPoor) && !b.HasKeyword(ClothingRich) ||
                   !a.HasKeyword(ClothingRich) && !b.HasKeyword(ClothingPoor);
        }

        private static List<IFormLink<IKeywordGetter>> _clothingBodySlots = new()
        {
            ClothingCirclet,
            ClothingFeet,
            ClothingBody,
            ClothingHead,
            ClothingFeet,
            ClothingNecklace,
            ClothingRing,
            ArmorBoots,
            ArmorCuirass,
            ArmorHelmet,
            ArmorShield,
            ArmorGauntlets
        };


        private bool DoClothingPicesHaveSameSlot(IArmorGetter a, IArmorGetter b)
        {
            var empty = Array.Empty<IFormLink<IKeywordGetter>>();
            var matching = a.Keywords?.Union(b.Keywords?.Union(_clothingBodySlots) ?? empty) ?? empty;
            return matching.Any();
        }


    }
}