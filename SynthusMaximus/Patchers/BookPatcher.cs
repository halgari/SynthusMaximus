﻿using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using SynthusMaximus.Data.Enums;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Weapon;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.EquipType;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Dragonborn.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Perk;
using static SynthusMaximus.Data.Statics;


namespace SynthusMaximus.Patchers
{
    public class BookPatcher : APatcher<BookPatcher>
    {
        public BookPatcher(ILogger<BookPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        public override void RunPatcher()
        {
            var spells = Mods.Spell().WinningOverrides().ToDictionary(s => s.FormKey);
            foreach (var b in Mods.Book().WinningOverrides())
            {
                try
                {
                    if (!(b.Teaches is IBookSpellGetter spg)) continue;
                    var sp = spells[spg.Spell.FormKey];

                    var createStaff = !(Storage.StaffCraftingExclusions.IsExcluded(b) ||
                                      Storage.StaffCraftingExclusions.IsExcluded(sp));
                    var createScroll = !(Storage.ScrollCraftingExclusions.IsExcluded(b) ||
                                       Storage.ScrollCraftingExclusions.IsExcluded(sp));
                    var distribute = !(Storage.SpellDistributionExclusions.IsExcluded(b) ||
                                       Storage.SpellDistributionExclusions.IsExcluded(sp));

                    if (createStaff)
                    {
                        var st = GenerateStaff(sp, sp.NameOrThrow());
                        GenerateStaffCraftingRecipe(st, sp, b);
                    }

                    if (createScroll)
                    {
                        var scroll = GenerateScroll(sp);
                        GenerateScrollCraftingRecipe(sp, scroll);
                    }

                    if (distribute)
                        DistributeBookOnLeveledLists(distribute);

                }
                catch (Exception ex)
                {
                    
                }
                
            }
        }

        private void DistributeBookOnLeveledLists(bool distribute)
        {
            throw new NotImplementedException();
        }

        private IConstructibleObjectGetter? GenerateScrollCraftingRecipe(ISpellGetter sp, IScrollGetter sc)
        {
            var perk = GetRequiredScrollCraftingPerk(sp);
            if (perk == null)
                return null;

            var cobj = Patch.ConstructibleObjects.AddNew();
            cobj.SetEditorID(SPrefixPatcher + SPrefixCrafting + SPrefixScroll + sc.NameOrThrow(), sp);
            cobj.CreatedObject.SetTo(sc);
            cobj.CreatedObjectCount = 1;
            
            cobj.AddCraftingPerkCondition(perk);
            cobj.AddCraftingRequirement(Inkwell01, 1);
            cobj.AddCraftingRequirement(PaperRoll, 1);
            cobj.AddCraftingSpellCondition(sp);

            return cobj;
        }

        private Dictionary<SpellTier, IFormLink<IPerkGetter>> SpellTierToPerk = new()
        {
            {SpellTier.Novice, xMAENCBasicScripture},
            {SpellTier.Apprentice, xMAENCBasicScripture},
            {SpellTier.Adept, xMAENCAdvancedScripture},
            {SpellTier.Expert, xMAENCElaborateScripture},
            {SpellTier.Master, xMAENCSagesScripture}
        };

        private IFormLink<IPerkGetter>? GetRequiredScrollCraftingPerk(ISpellGetter sp)
        {
            var tier = GetSpellTier(sp);
            return tier == null ? null : SpellTierToPerk[(SpellTier)tier!];
        }

        private SpellTier? GetSpellTier(ISpellGetter sp)
        {
            var level = sp.Effects.Select(e => e.BaseEffect.Resolve(State.LinkCache))
                .Select(e => e.MinimumSkillLevel)
                .Max();
            return SpellTiers.FromLevel((int)level);
        }

        private IScrollGetter? GenerateScroll(ISpellGetter sp)
        {
            if (sp.CastType == CastType.Concentration)
                return null;

            var newScroll = Patch.Scrolls.AddNew();
            newScroll.SetEditorID(SPrefixPatcher + SPrefixScroll + sp.NameOrEmpty(), sp);
            newScroll.CastDuration = sp.CastDuration;
            newScroll.CastType = sp.CastType;
            newScroll.TargetType = sp.TargetType;
            newScroll.EquipmentType.SetTo(sp.EquipmentType);
            newScroll.Type = sp.Type;
            newScroll.Name = sp.NameOrEmpty() + " [" + Storage.GetOutputString(SScroll) + "]";
            newScroll.Effects.AddRange(sp.Effects.Select(e => e.DeepCopy()));
            return newScroll;
        }

        private Dictionary<ActorValue, IFormLink<IWeaponGetter>> EmptyStaves =
            new()
            {
                {ActorValue.Destruction, StaffTemplateDestruction},
                {ActorValue.Conjuration, StaffTemplateConjuration},
                {ActorValue.Alteration, StaffTemplateAlteration},
                {ActorValue.Illusion, StaffTemplateIIllusion},
                {ActorValue.Restoration, StaffTemplateRestoration}
            };

        private IConstructibleObjectGetter? GenerateStaffCraftingRecipe(IWeaponGetter st, ISpellGetter sp, IBookGetter b)
        {
            var newRecipe = Patch.ConstructibleObjects.AddNew();
            newRecipe.SetEditorID(SPrefixPatcher + SPrefixCrafting + SPrefixStaff + sp.NameOrEmpty(), sp);
            newRecipe.WorkbenchKeyword.SetTo(DLC2StaffEnchanter);
            newRecipe.CreatedObject.SetTo(st);
            newRecipe.CreatedObjectCount = 1;
            
            newRecipe.AddCraftingPerkCondition(xMAENCStaffaire);
            newRecipe.AddCraftingSpellCondition(sp);

            var school = GetSchool(sp);
            var staff = EmptyStaves[school ?? ActorValue.Destruction];
            newRecipe.AddCraftingRequirement(staff, 1);
            return newRecipe;
        }

        private IObjectEffectGetter? GenerateStaffEnchantment(ISpellGetter sp)
        {
            if (sp.CastType == CastType.ConstantEffect)
                return null;
            if (sp.TargetType == TargetType.Self)
                return null;
            if (sp.EquipmentType.FormKey == BothHands.FormKey)
                return null;

            var newENCH = Patch.ObjectEffects.AddNew();
            newENCH.SetEditorID(SPrefixPatcher + SPrefixEnchantment + sp.NameOrEmpty(), sp);
            newENCH.TargetType = sp.TargetType;
            newENCH.CastType = sp.CastType;
            newENCH.Name = SPrefixEnchantment + sp.NameOrEmpty();
            newENCH.EnchantmentCost = Math.Min(100, Math.Max(sp.BaseCost, 50));
            newENCH.Effects.AddRange(sp.Effects.Select(e => e.DeepCopy()));

            return newENCH;
        }
        
        public ActorValue? GetSchool(ISpellGetter sp)
        {
            foreach (var me in sp.Effects)
            {
                var resolved = me.BaseEffect.Resolve(State.LinkCache);
                if (EmptyStaves.ContainsKey(resolved.MagicSkill))
                    return resolved.MagicSkill;
            }

            return null;
        }

        private IWeaponGetter? GenerateStaff(ISpellGetter sp, string name)
        {
            var ench = GenerateStaffEnchantment(sp);
            if (ench == null)
            {
                Logger.LogWarning("Could not create staff enchantment for {EditorID}", sp.EditorID);
                return null;
            }

            var av = GetSchool(sp);
            if (av == null)
            {
                Logger.LogWarning("Could not get school for {EditorID}", sp.EditorID);
                return null;
            }

            var baseStaff = EmptyStaves[(ActorValue)av].Resolve(State.LinkCache);

            var newStaff = Patch.Weapons.DuplicateInAsNewRecord(baseStaff);
            newStaff.SetEditorID(SPrefixPatcher + SPrefixStaff + sp.NameOrThrow(), sp);
            newStaff.ObjectEffect.SetTo(ench);
            newStaff.EnchantmentAmount = 2500;
            newStaff.Name = Storage.GetOutputString(SStaff) + " [" + name + "]";
            return newStaff;
        }
    }
}