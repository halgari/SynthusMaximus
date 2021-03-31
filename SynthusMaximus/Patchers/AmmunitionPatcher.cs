using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynthusMaximus.Data;
using SynthusMaximus.Data.DTOs.Ammunition;
using SynthusMaximus.Data.Enums;
using static SynthusMaximus.Data.Statics;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Ingredient;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Ingestible;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Keyword;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Light;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Perk;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.MiscItem;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.PerkusMaximus_Master.Explosion;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.MiscItem;

namespace SynthusMaximus.Patchers
{
    public class AmmunitionPatcher : APatcher<AmmunitionPatcher>
    {
        private Dictionary<FormKey, IProjectileGetter> _projectiles;

        public AmmunitionPatcher(ILogger<AmmunitionPatcher> logger, DataStorage storage, IPatcherState<ISkyrimMod, ISkyrimModGetter> state) : base(logger, storage, state)
        {
        }

        public override void RunPatcher()
        {
            _projectiles = Mods.Projectile().WinningOverrides().ToDictionary(p => p.FormKey);
            
            foreach (var ammo in Mods.Ammunition().WinningOverrides())
            {
                try
                {
                    if (!ShouldPatch(ammo))
                        continue;

                    var at = Storage.GetAmmunitionType(ammo);
                    if (at == null)
                    {
                        SkipRecord(ammo, "no ammunition type");
                        continue;
                    }

                    var am = Storage.GetAmmunitionMaterial(ammo);
                    if (am == null)
                    {
                        SkipRecord(ammo, "no material type");
                        continue;
                    }

                    if (am.Multiply)
                    {
                        switch (at.Type)
                        {
                            case BaseAmmunitionType.Arrow:
                                CreateArrowVariants(ammo, am, at);
                                break;
                            case BaseAmmunitionType.Bolt:
                                CreateBoltVariants(ammo, am, at);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    if (Storage.UseWarrior)
                    {
                        var na = Patch.Ammunitions.GetOrAddAsOverride(ammo);
                        var modifiers = Storage.GetAmmunitionModifiers(na).ToArray();
                        na.Damage = at.DamageBase + am.DamageModifier + modifiers.Sum(m => m.DamageModifier);
                        PatchProjectile(na, am, at, modifiers);
                    }


                }
                catch (Exception ex)
                {
                    ReportFailed(ex, ammo);
                }
            }
            
        }

        private void CreateBoltVariants(IAmmunitionGetter baseAmmo, AmmunitionMaterial am, AmmunitionType at)
        {
            var strongAmmo = CreateAmmo(baseAmmo, am, at, SAmmoStrong, null);
            CreateAmmoCraftingRecipeVariants(baseAmmo, strongAmmo, new[] {IngotIron}, new[] {xMARANAdvancedMissilecraft0});
            
            var strongestAmmo = CreateAmmo(baseAmmo, am, at, SAmmoStrong, null);
            CreateAmmoCraftingRecipeVariants(strongAmmo, strongestAmmo, new[] {IngotSteel}, new[] {xMARANAdvancedMissilecraft0});

            foreach (var a in new[] {baseAmmo, strongAmmo, strongestAmmo})
            {


                var newPoisonAmmo = CreateAmmo(a, am, at, SAmmoPoison, SAmmoPoisonDesc,
                    projectileFn: p =>
                    {
                        p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                        p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                        p.Explosion.SetTo(xMAALCPoisonBurstAmmoPoisonExplosion);
                    });
                CreateAmmoCraftingRecipeVariants(a, newPoisonAmmo, new[] {deathBell}, new[] {xMAALCPoisonBurst});

                var newFireAmmo = CreateAmmo(a, am, at, SAmmoFire, SAmmoFireDesc,
                    projectileFn: p =>
                    {
                        p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                        p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                        p.Explosion.SetTo(xMAALCElementalBurstExplosionFire);
                    });
                CreateAmmoCraftingRecipeVariants(a, newFireAmmo, new[] {FireSalts}, new[] {xMAALCElementalBombard});

                var newFrostAmmo = CreateAmmo(a, am, at, SAmmoFrost, SAmmoFrostDesc,
                    projectileFn: p =>
                    {
                        p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                        p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                        p.Explosion.SetTo(xMAALCElementalBurstExplosionFrost);
                    });
                CreateAmmoCraftingRecipeVariants(a, newFrostAmmo, new[] {FrostSalts}, new[] {xMAALCElementalBombard});

                var newShockAmmo = CreateAmmo(a, am, at, SAmmoShock, SAmmoShockDesc,
                    projectileFn: p =>
                    {
                        p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                        p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                        p.Explosion.SetTo(xMAALCElementalBurstExplosionShock);
                    });
                CreateAmmoCraftingRecipeVariants(a, newShockAmmo, new[] {VoidSalts}, new[] {xMAALCElementalBombard});

                var newBarbedAmmo = CreateAmmo(a, am, at, SAmmoBarbed, SAmmoBarbedDesc,
                    projectileFn: p =>
                    {
                        p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                        p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                        p.Explosion.SetTo(xMARANAdvancedMissilecraft1BarbedExplosion);
                    });
                CreateAmmoCraftingRecipeVariants(a, newBarbedAmmo,
                    new IFormLink<IItemGetter>[] {IngotIron, IngotSteel},
                    new[] {xMARANAdvancedMissilecraft1});

                var newExplosiveAmmo = CreateAmmo(a, am, at, SAmmoExplosive, SAmmoExplosiveDesc,
                    projectileFn: p =>
                    {
                        p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                        p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                        p.Explosion.SetTo(xMAALCAdvancedExplosivesMissileExplosion);
                    });
                CreateAmmoCraftingRecipeVariants(a, newExplosiveAmmo, new IFormLink<IItemGetter>[] {Ale, LeatherStrips},
                    new[] {xMAALCFuse});

                var newTimebombAmmo = CreateAmmo(a, am, at, SAmmoTimebomb, SAmmoTimebombDesc,
                    projectileFn: p =>
                    {
                        p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                        p.Flags.SetFlag(Projectile.Flag.AltTrigger, true);
                        p.ExplosionAltTriggerTimer = TimebombTimer;
                        p.Explosion.SetTo(xMAALCAdvancedExplosivesMissileExplosion);
                    });
                CreateAmmoCraftingRecipeVariants(a, newTimebombAmmo,
                    new IFormLink<IItemGetter>[] {Ale, LeatherStrips, Charcoal},
                    new[] {xMAALCAdvancedExplosives});
            }
        }

        private void CreateArrowVariants(IAmmunitionGetter a, AmmunitionMaterial am, AmmunitionType at)
        {
            var newPoisonAmmo = CreateAmmo(a, am, at, SAmmoPoison, SAmmoPoisonDesc,
                projectileFn: p =>
                {
                    p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                    p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                    p.Explosion.SetTo(xMAALCPoisonBurstAmmoPoisonExplosion);
                });
            CreateAmmoCraftingRecipeVariants(a, newPoisonAmmo, new[] {deathBell}, new[] {xMAALCPoisonBurst});
                
            var newFireAmmo = CreateAmmo(a, am, at, SAmmoFire, SAmmoFireDesc,
                projectileFn: p =>
                {
                    p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                    p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                    p.Explosion.SetTo(xMAALCElementalBurstExplosionFire);
                });
            CreateAmmoCraftingRecipeVariants(a, newFireAmmo, new[] {FireSalts}, new[] {xMAALCElementalBombard});
            
            var newFrostAmmo = CreateAmmo(a, am, at, SAmmoFrost, SAmmoFrostDesc,
                projectileFn: p =>
                {
                    p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                    p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                    p.Explosion.SetTo(xMAALCElementalBurstExplosionFrost);
                });
            CreateAmmoCraftingRecipeVariants(a, newFrostAmmo, new[] {FrostSalts}, new[] {xMAALCElementalBombard});
            
            var newShockAmmo = CreateAmmo(a, am, at, SAmmoShock, SAmmoShockDesc,
                projectileFn: p =>
                {
                    p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                    p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                    p.Explosion.SetTo(xMAALCElementalBurstExplosionShock);
                });
            CreateAmmoCraftingRecipeVariants(a, newShockAmmo, new[] {VoidSalts}, new[] {xMAALCElementalBombard});
            
            var newLightsourceAmmo = CreateAmmo(a, am, at, SAmmoLightsource, SAmmoLightsourceDesc,
                projectileFn: p =>
                {
                    p.Light.SetTo(xMASNEThiefsToolboxLightsourceArrowLight);
                });
            CreateAmmoCraftingRecipeVariants(a, newLightsourceAmmo, new IFormLink<IItemGetter>[] {FireflyThorax, LeatherStrips}, 
                new[] {xMASNEThiefsToolbox0});
            
            var newExplosiveAmmo = CreateAmmo(a, am, at, SAmmoExplosive, SAmmoExplosiveDesc,
                projectileFn: p =>
                {
                    p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                    p.Flags.SetFlag(Projectile.Flag.AltTrigger, false);
                    p.Explosion.SetTo(xMAALCAdvancedExplosivesMissileExplosion);
                });
            CreateAmmoCraftingRecipeVariants(a, newExplosiveAmmo, new IFormLink<IItemGetter>[] {Ale, LeatherStrips}, 
                new[] {xMAALCFuse});
            
            var newTimebombAmmo = CreateAmmo(a, am, at, SAmmoTimebomb, SAmmoTimebombDesc,
                projectileFn: p =>
                {
                    p.Flags.SetFlag(Projectile.Flag.Explosion, true);
                    p.Flags.SetFlag(Projectile.Flag.AltTrigger, true);
                    p.ExplosionAltTriggerTimer = TimebombTimer;
                    p.Explosion.SetTo(xMAALCAdvancedExplosivesMissileExplosion);
                });
            CreateAmmoCraftingRecipeVariants(a, newTimebombAmmo, new IFormLink<IItemGetter>[] {Ale, LeatherStrips, Charcoal}, 
                new[] {xMAALCAdvancedExplosives});
            
            
            
        }

        private void CreateAmmoCraftingRecipeVariants(IAmmunitionGetter baseAmmo, IAmmunition resultAmmo, 
            IReadOnlyList<IFormLink<IItemGetter>> ingredients,
            IEnumerable<IFormLink<IPerkGetter>> requiredPerks)
        {
            var allPerks = requiredPerks.ToList();
            CreateAmmoCraftingRecipe(baseAmmo, resultAmmo, EnhancementIn, EnhancementOut, ingredients,
                allPerks, xMAALCSkilledEnhancer0, CraftingSmithingForge);
            
            allPerks.Add(xMAALCSkilledEnhancer0);
            
            CreateAmmoCraftingRecipe(baseAmmo, resultAmmo, EnhancementIn, EnhancementOut, ingredients,
                allPerks, xMAALCSkilledEnhancer1, CraftingSmithingForge);
            
            allPerks.Add(xMAALCSkilledEnhancer1);
            
            CreateAmmoCraftingRecipe(baseAmmo, resultAmmo, EnhancementIn, EnhancementOut, ingredients,
                allPerks, null, CraftingSmithingForge);
        }

        private void CreateAmmoCraftingRecipe(IAmmunitionGetter baseAmmo, IAmmunitionGetter resultAmmo,
            int inputNum, ushort outputNum, IEnumerable<IFormLink<IItemGetter>> ingredients,
            IEnumerable<IFormLink<IPerkGetter>> requiredPerks, IFormLink<IPerkGetter>? blockerPerk,
            IFormLink<IKeywordGetter> craftingBenchKw)
        {
            string eid = "";
            if (blockerPerk != null)
            {
                eid = SPrefixPatcher + SPrefixAmmunition + SPrefixCrafting + resultAmmo.EditorID +
                          baseAmmo.FormKey + blockerPerk.FormKey;
            }
            else
            {
                eid = SPrefixPatcher + SPrefixAmmunition + SPrefixCrafting + resultAmmo.EditorID + baseAmmo.FormKey;
            }

            var newrec = Patch.ConstructibleObjects.AddNew();
            newrec.EditorID = eid;
            newrec.WorkbenchKeyword.SetTo(craftingBenchKw);
            newrec.CreatedObject.SetTo(resultAmmo);
            newrec.CreatedObjectCount = outputNum;
            newrec.AddCraftingRequirement(baseAmmo, inputNum);

            foreach (var i in ingredients)
            {
                newrec.AddCraftingRequirement(i, 1);
            }

            foreach (var perk in requiredPerks)
            {
                newrec.AddCraftingPerkCondition(perk);
            }
            
            if (blockerPerk != null)
                newrec.AddCraftingPerkCondition(blockerPerk, false);
            
            newrec.AddCraftingInventoryCondition(baseAmmo, 1);



        }

        private IAmmunition CreateAmmo(IAmmunitionGetter a, AmmunitionMaterial am, AmmunitionType at, string ammoName,
            string? ammoDesc,
            Action<Ammunition>? ammoFn = null, Action<Projectile>? projectileFn = null)
        {
            var na = Patch.Ammunitions.DuplicateInAsNewRecord(a);
            na.EditorID = SPrefixPatcher + SPrefixAmmunition + a.EditorID + ammoName + a.FormKey;
            na.Name = $"{a.NameOrThrow()} - {Storage.GetOutputString(ammoName)}";
            
            if (ammoDesc != null) 
                na.Description = Storage.GetOutputString(ammoDesc);

            var p = _projectiles[na.Projectile.FormKey];
            var np = Patch.Projectiles.DuplicateInAsNewRecord(p);
            _projectiles[np.FormKey] = np;
            np.EditorID = SPrefixPatcher + SPrefixProjectile + na.EditorID + a.FormKey;
            na.Projectile.SetTo(np);
            
            var amod = Storage.GetAmmunitionModifiers(na).ToArray();
            projectileFn?.Invoke(np);

            if (Storage.UseWarrior)
            {
                na.Damage = at.DamageBase + am.DamageModifier + amod.Sum(m => m.DamageModifier);
                PatchProjectile(na, am, at, amod);
            }

            return na;
        }

        private void PatchProjectile(Ammunition na, AmmunitionMaterial am, AmmunitionType at, AmmunitionModifier[] amod)
        {
            var p = Patch.Projectiles.GetOrAddAsOverride(_projectiles[na.Projectile.FormKey]);
            p.Speed = at.SpeedBase + am.SpeedModifier + amod.Sum(m => m.SpeedModifier);
            p.Gravity = at.GravityBase + am.GravityModifier + amod.Sum(m => m.GravityModifier);
            p.Range = at.RangeBase + am.RangeModifier + amod.Sum(m => m.RangeModifier);
        }


        private static bool ShouldPatch(IAmmunitionGetter ammo)
        {
            if (string.IsNullOrEmpty(ammo.NameOrEmpty()))
                return false;

            return !ammo.Flags.HasFlag(Ammunition.Flag.NonPlayable);
        }
    }
}