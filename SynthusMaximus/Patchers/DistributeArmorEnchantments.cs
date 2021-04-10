using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using SynthusMaximus.Data.DTOs;
using SynthusMaximus.Support.RunSorting;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;
using static SynthusMaximus.Data.Statics;

namespace SynthusMaximus.Patchers
{
    
    [RunAfter(typeof(ArmorPatcher))]
    [RunAfter(typeof(FillArmorListsWithSimilars))]
    public class DistributeArmorEnchantments : ADistributeItemEnchantments<DistributeArmorEnchantments, IArmorGetter>
    {
        public DistributeArmorEnchantments(ILogger<DistributeArmorEnchantments> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        protected override IFormLinkNullableGetter<IArmorGetter> GetTemplate(IArmorGetter i)
        {
            return i.TemplateArmor;
        }

        protected override IFormLinkNullableGetter<IEffectRecordGetter> GetEnchantment(IArmorGetter i)
        {
            return i.ObjectEffect;
        }
        
        protected override MajorRecordExclusionList<ILeveledItemGetter> GetDistributionExclusionList()
        {
            return Storage.DistributionExclusionsArmor;
        }

        protected override ParallelQuery<IArmorGetter> AllItems()
        {
            return Mods.Armor().WinningOverrides().AsParallel().Where(a => Storage.GetArmorMaterial(a) != null);
        }

        protected override ExclusionList<IArmorGetter> GetEnchantmentExclusionList()
        {
            return Storage.EnchantmentArmorExclusions;
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
            newArmor.Name = Storage.GetLocalizedEnchantmentNameArmor(template, e);
            return newArmor;
        }
    }
}