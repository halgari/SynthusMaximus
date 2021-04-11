using System.Collections.Generic;
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
    [RunAfter(typeof(WeaponPatcher))]
    public class FillWeaponListsWithSimilars : AFillWithSimilars<FillArmorListsWithSimilars, IWeaponGetter>
    {
        public FillWeaponListsWithSimilars(ILogger<FillArmorListsWithSimilars> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        protected override IEnumerable<IWeaponGetter> GetSimilars(IWeaponGetter w1)
        {
            var mat1 = Storage.GetWeaponMaterial(w1);
            var type1 = Storage.GetWeaponType(w1);

            if (mat1 == null || type1 == null) yield break;
            
            foreach (var w2 in Mods.Weapon().WinningOverrides())
            {
                var mat2 = Storage.GetWeaponMaterial(w2);
                var type2 = Storage.GetWeaponType(w2);
                if (mat2 == null || type2 == null) continue;
                
                if (mat1.Type.Data?.TemperingInput?.FormKey != mat2.Type.Data?.TemperingInput?.FormKey) continue;
                if (type1.BaseWeaponType.Data!.School != type2.BaseWeaponType.Data!.School) continue;
                if (w1.ObjectEffect.FormKey != w2.ObjectEffect.FormKey) continue;
                
                if (!WeaponPatcher.DoWeaponsContainClasses(w1, w2)) continue;
                yield return w2;
            }
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

        protected override ExclusionList<IWeaponGetter> GetEnchantmentExclusionList()
        {
            return Storage.EnchantmentWeaponExclusions;
        }
    }
}