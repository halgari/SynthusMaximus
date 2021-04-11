using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;
using SynthusMaximus.Data.DTOs;
using SynthusMaximus.Support.RunSorting;
using static SynthusMaximus.Data.Statics;

namespace SynthusMaximus.Patchers
{
    [RunAfter(typeof(FillWeaponListsWithSimilars))]
    public class DistributeWeaponEnchantments : ADistributeItemEnchantments<DistributeWeaponEnchantments, IWeaponGetter>
    {
        public DistributeWeaponEnchantments(ILogger<DistributeWeaponEnchantments> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        protected override IFormLinkNullableGetter<IWeaponGetter> GetTemplate(IWeaponGetter i) => i.Template;

        protected override IFormLinkNullableGetter<IEffectRecordGetter> GetEnchantment(IWeaponGetter i) => i.ObjectEffect;

        protected override IWeaponGetter CreateItemFromTemplate(IWeaponGetter template, IWeaponGetter like, IFormLink<IObjectEffectGetter> e)
        {
            var resolved = e.Resolve(State.LinkCache);
            var newArmor = Patch.Weapons.DuplicateInAsNewRecord(template);
            newArmor.SetEditorID(
                SPrefixPatcher + SPrefixWeapon + template.NameOrEmpty() + resolved.NameOrEmpty(), resolved);
            newArmor.Template.SetTo(template);
            newArmor.ObjectEffect.SetTo(e);
            newArmor.BasicStats!.Value = like.BasicStats!.Value;
            newArmor.EnchantmentAmount = like.EnchantmentAmount;
            newArmor.Name = Storage.GetLocalizedEnchantmentName(template, e);
            return newArmor;
        }

        protected override MajorRecordExclusionList<ILeveledItemGetter> GetDistributionExclusionList()
        {
            return Storage.DistributionExclusionsWeaponsEnchanted;
        }

        protected override ParallelQuery<IWeaponGetter> AllItems()
        {
            return Mods.Weapon().WinningOverrides()
                .AsParallel()
                .Where(w => Storage.GetWeaponMaterial(w) != null)
                .Where(w => Storage.GetWeaponType(w) != null);
        }

        protected override ExclusionList<IWeaponGetter> GetEnchantmentExclusionList()
        {
            return Storage.EnchantmentWeaponExclusions;
        }
    }
}