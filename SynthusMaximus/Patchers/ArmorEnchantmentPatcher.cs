using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynthusMaximus.Data;

namespace SynthusMaximus.Patchers
{
    public class ArmorEnchantmentPatcher : APatcher<ArmorEnchantmentPatcher>, IRunAfter<ArmorPatcher>
    {
        private int _totalAdded;

        public ArmorEnchantmentPatcher(ILogger<ArmorEnchantmentPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        public override void RunPatcher()
        {
            RunDirectMatch();
            Logger.LogInformation("Added {total} enchanted armors to leveled lists", _totalAdded);
        }

        private void RunDirectMatch()
        {
            foreach (var armor in Mods.Armor().WinningOverrides())
            {
                var material = Storage.GetArmorMaterial(armor);
                if (material == null) continue;
                
                if (armor.ObjectEffect.IsNull) continue;
                if (Storage.EnchantmentArmorExclusions.IsExcluded(armor)) continue;

                if (armor.TemplateArmor.IsNull) continue;

                var template = armor.TemplateArmor.Resolve(State.LinkCache);
                if (Storage.EnchantmentArmorExclusions.IsExcluded(template)) continue;
                
                
                foreach (var other in Storage.DirectEnchantmentBindings[new FormLink<IObjectEffectGetter>(armor.ObjectEffect.FormKey)])
                {
                    var newArmor = CreateEnchantedArmorFromTemplate(template, armor, other.New);
                    foreach (var lst in Mods.LeveledItem().WinningOverrides())
                    {
                        if (Storage.DistributionExclusionsArmor.IsExcluded(lst)) continue;
                        if (lst!.Flags.HasFlag(LeveledItem.Flag.UseAll)) continue;
                        
                        if (lst.Entries == null) continue;

                        var toAdd = lst.Entries
                            .Where(e => e.Data!.Reference.FormKey == armor.FormKey)
                            .Select(e => new LeveledItemEntry
                            {
                                Data = new LeveledItemEntryData()
                                {
                                    Reference = new FormLink<IItemGetter>(newArmor.FormKey),
                                    Count = e.Data!.Count,
                                    Level = e.Data.Level
                                }
                            })
                            .ToList();

                        if (toAdd.Count == 0) continue;
                        _totalAdded += toAdd.Count;
                        var lstm = Patch.LeveledItems.GetOrAddAsOverride(lst);
                        lstm.Entries!.AddRange(toAdd);
                    }
                }
            }
        }

        private IArmorGetter CreateEnchantedArmorFromTemplate(IArmorGetter template, IArmorGetter like, IFormLink<IObjectEffectGetter> e)
        {
            var newArmor = Patch.Armors.DuplicateInAsNewRecord(template);
            newArmor.TemplateArmor.SetTo(template);
            newArmor.ObjectEffect.SetTo(e);
            newArmor.Value = like.Value;
            newArmor.Name = Storage.GetLocalizedEnchantmentNameArmor(template, e);
            return newArmor;
        }
    }
}