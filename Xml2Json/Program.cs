using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Xml2Json
{
    class Program
    {
        public static string OutputFolder = @"..\SynthusMaximus\config\PerkusMaximus_Master";
        static void Main(string[] args)
        {
            var leveledLists = XElement.Load("LeveledLists.xml");

            ExtractExclusionList(leveledLists, "distribution_exclusions_weapon_regular", @"exclusions\distributionExclusionsWeaponRegular.json");
            ExtractExclusionList(leveledLists, "distribution_exclusions_armor", @"exclusions\distributionExclusionsArmor.json");
            ExtractExclusionList(leveledLists, "distribution_exclusions_list_regular", @"exclusions\distributionExclusionsWeaponListRegular.json");
            ExtractExclusionList(leveledLists, "distribution_exclusions_spell", @"exclusions\distributionExclusionsSpell.json");
            ExtractExclusionList(leveledLists, "distribution_exclusions_weapon_enchanted", @"exclusions\distributionExclusionsWeaponEnchanted.json");
            
            var alchemy = XElement.Load("Alchemy.xml");
            ExtractExclusionList(alchemy, "potion_exclusions", @"exclusions\potionExclusions.json");
            ExtractExclusionList(alchemy, "ingredient_exclusions", @"exclusions\ingredientExclusions.json");
            ExtractBindingList(alchemy, "alchemy_effect_bindings", "alchemy_effects", @"alchemy\alchemyEffects.json",
                new()
                {
                    {"identifier", typeof(string)},
                    {"baseMagnitude", typeof(float)},
                    {"baseDuration", typeof(float)},
                    {"baseCost", typeof(float)},
                    {"allowIngredientVariation", typeof(bool)},
                    {"allowPotionMultiplier", typeof(bool)}
                });
            
            ExtractBindingList(alchemy, "ingredient_variation_bindings", "ingredient_variations", @"alchemy\ingredientVariations.json",
                new ()
                {
                    {"identifier", typeof(string)},
                    {"multiplierMagnitude", typeof(float)},
                    {"multiplierDuration", typeof(float)}
                });
            
            ExtractBindingList(alchemy, "potion_multiplier_bindings", "potion_multipliers", @"alchemy\potionMultipliers.json",
                new ()
                {
                    {"identifier", typeof(string)},
                    {"multiplierMagnitude", typeof(float)},
                    {"multiplierDuration", typeof(float)}
                });

            var ammo = XElement.Load("Ammunition.xml");
            ExtractBindingList(ammo, "ammunition_type_bindings", "ammunition_types", @"ammunition\ammunitionTypes.json", 
                    new ()
                    {
                        {"identifier", typeof(string)},
                        {"type", typeof(string)},
                        {"damageBase", typeof(float)},
                        {"rangeBase", typeof(float)},
                        {"speedBase", typeof(float)},
                        {"gravityBase", typeof(float)}
                    });
                
            ExtractBindingList(ammo, "ammunition_modifier_bindings", "ammunition_modifiers", @"ammunition\ammunitionModifiers.json", 
                    new ()
                    {
                        {"identifier", typeof(string)},
                        {"damageModifier", typeof(float)},
                        {"rangeModifier", typeof(float)},
                        {"speedModifier", typeof(float)},
                        {"gravityModifier", typeof(float)}
                    });
            ExtractExclusionList(ammo, "ammunition_exclusions_multiplication", @"ammunition\ammunitionExclusionsMultiplication.json");
            
            ExtractBindingList(ammo, "ammunition_material_bindings", "ammunition_materials", @"ammunition\ammunitionMaterials.json",
                new () {
                    {"identifier", typeof(string)},
                    {"damageModifier", typeof(float)},
                    {"rangeModifier", typeof(float)},
                    {"speedModifier", typeof(float)},
                    {"gravityModifier", typeof(float)},
                    {"multiply", typeof(bool)}
                });
            
            // Enchanting 
            
            var ench = XElement.Load("Enchanting.xml");
            ExtractExclusionList(ench, "scroll_crafting_exclusions", @"exclusions\scrollCrafting.json");
            ExtractExclusionList(ench, "staff_crafting_exclusions", @"exclusions\staffCrafting.json");
            ExtractExclusionList(ench, "staff_crafting_disable_crafting_exclusions", @"exclusions\staffCraftingDisableCraftingExclusions.json");
            ExtractExclusionList(ench, "enchantment_armor_exclusions", @"exclusions\enchantmentArmorExclusions.json");
            ExtractExclusionList(ench, "enchantment_weapon_exclusions", @"exclusions\enchantmentWeaponExclusions.json");
            ExtractEnchantmentReplacers(ench, "list_enchantment_bindings", @"enchanting\listEnchantmentBindings.json");
            ExtractEnchantmentDirectBindings(ench, "direct_enchantment_bindings", @"enchanting\directEnchantmentBindings.json");
            ExtractEnchantmentNameBindings(ench, "enchantment_name_bindings", @"enchanting\nameBindings.json");
            ExtractCompilexExclusionList(ench, "similarity_exclusions_armor",
                @"enchanting\similaritiesExclusionsArmor.json");
            ExtractCompilexExclusionList(ench, "similarity_exclusions_armor",
                @"enchanting\similaritiesExclusionsWeapon.json");

            
            var npcs = XElement.Load("NPC.xml");
            ExtractExclusionList(npcs, "npc_exclusions", @"exclusions\npcs.json");
            ExtractExclusionList(npcs, "race_exclusions", @"exclusions\race.json");

        }

        private static void ExtractCompilexExclusionList(XElement element, string listName, string fileName)
        {
            var bindings = element.Descendants().Where(e => e.Name == listName);
            var results = new List<Dictionary<string, string>>();
            foreach (var binding in bindings.Descendants().Where(e => e.Name == "complex_exclusion"))
            {
                var ex0 = binding.Descendants().First(d => d.Name == "exclusion_0");
                var ex1 = binding.Descendants().First(d => d.Name == "exclusion_1");
                
                ParseExclusion(ex0, out var text0, out var target0);
                ParseExclusion(ex1, out var text1, out var target1);
                results.Add(new Dictionary<string, string>()
                {
                    {"textA", text0},
                    {"targetA", target0},
                    {"textB", text1},
                    {"targetB", target1},
                });
            }
            WriteFile(fileName, results);
        }

        private static void ExtractEnchantmentNameBindings(XElement element, string listName, string fileName)
        {
            var bindings = element.Descendants().Where(e => e.Name == listName);
            var results = new List<Dictionary<string, string>>();
            foreach (var binding in bindings.Descendants().Where(e => e.Name == "enchantment_name_binding"))
            {
                var edid = binding.Descendants().First(d => d.Name == "edidEnchantment").Value;
                var name = binding.Descendants().First(d => d.Name == "name").Value;
                var type = binding.Descendants().First(d => d.Name == "type").Value;

                name = type switch
                {
                    "SUFFIX" => "{0} " + name,
                    "PREFIX" => name + " {0}",
                    "NONE" => "{0}",
                    _ => throw new NotImplementedException($"{type} not implemented")
                };
                results.Add(new Dictionary<string, string>()
                {
                    {"name", name},
                    {"edid", edid}
                });
            }
            WriteFile(fileName, results);
        }

        private static void ExtractEnchantmentDirectBindings(XElement element, string listName, string fileName)
        {
            var bindings = element.Descendants().Where(e => e.Name == listName);
            var results = new List<Dictionary<string, string>>();
            foreach (var binding in bindings.Descendants().Where(e => e.Name == "direct_enchantment_binding"))
            {
                var oldName = binding.Descendants().First(d => d.Name == "edidEnchantmentBase").Value;
                var newName = binding.Descendants().First(d => d.Name == "edidEnchantmentNew").Value;
                results.Add(new Dictionary<string, string>()
                {
                    {"base", oldName},
                    {"new", newName}
                });
            }
            
            WriteFile(fileName, results);
        }

        private static void ExtractEnchantmentReplacers(XElement element, string listName, string fileName)
        {
            var bindings = element.Descendants().Where(e => e.Name == listName);
            var results = new List<Dictionary<string, object>>();
            foreach (var binding in bindings.Descendants().Where(e => e.Name == "list_enchantment_binding"))
            {
                var entry = new Dictionary<string, object>();
                entry["fillListWithSimilars"] =
                    bool.Parse(binding.Descendants().First(e => e.Name == "fillListWithSimilars").Value);
                entry["edidList"] = binding.Descendants().First(e => e.Name == "edidList").Value;
                var list = new List<Dictionary<string, string>>();
                foreach (var replacement in binding.Descendants().Where(d => d.Name == "enchantment_replacer"))
                {
                    var baseId = replacement.Descendants().First(e => e.Name == "edidEnchantmentBase").Value;
                    var newId = replacement.Descendants().First(e => e.Name == "edidEnchantmentNew").Value;
                    list.Add(new Dictionary<string, string>()
                    {
                        {"edidBase", baseId},
                        {"edidNew", newId}
                    });
                }

                entry["enchantmentReplacers"] = list;
                results.Add(entry);
            }
            WriteFile(fileName, results);
        }

        private static void ExtractBindingList(XElement doc, string bindingName, string bindableName, string fileName,
            Dictionary<string, Type> members)
        {
            var bindings = ExtractBindingNames(doc, bindingName);
            var bindables = ExtractBindables(doc, bindableName, members);
            foreach (var b in bindables)
            {
                if (bindings.TryGetValue((string) b["identifier"], out var substrs))
                {
                    b["nameSubstrings"] = substrs;
                }
                else
                {
                    b["nameSubstrings"] = Array.Empty<string>();
                }
            }

            bindables = bindables
                .Select(b =>
                {
                    b["nameSubstrings"] = ((string[]) b["nameSubstrings"]).OrderBy(x => x).ToArray();
                    return b;
                })
                .OrderBy(b => b["identifier"]).ToList();
            

            WriteFile(fileName, bindables);
        }

        private static List<Dictionary<string, object>> ExtractBindables(XElement e, string bindableName, Dictionary<string,Type> members)
        {
            var bindables = e.Descendants().Where(e => e.Name == bindableName).Elements()
                .Select(c =>
                {
                    return members.Select(m => GetKV(c, m))
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                })
                .ToList();
            return bindables;

        }

        private static KeyValuePair<string, object> GetKV(XElement e, KeyValuePair<string, Type> kv)
        {
            var value = e.Elements().First(c => c.Name == kv.Key).Value;
            if (kv.Value == typeof(string))
                return new KeyValuePair<string, object>(kv.Key, value);
            if (kv.Value == typeof(float))
                return new KeyValuePair<string, object>(kv.Key, float.Parse(value));
            if (kv.Value == typeof(bool))
                return new KeyValuePair<string, object>(kv.Key, bool.Parse(value));
            throw new NotImplementedException();
        }

        private static Dictionary<string, string[]> ExtractBindingNames(XElement e, string bindingName)
        {
            var results = e.Descendants().Where(d => d.Name == bindingName)
                .SelectMany(d => d.Descendants().Where(c => c.Name == "binding"))
                .Select(node => (node.Descendants().First(d => d.Name == "identifier").Value,
                        node.Descendants().First(d => d.Name == "substring").Value))
                .ToArray();
            return results.GroupBy(d => d.Item1)
                .ToDictionary(d => d.Key, d => d.Select(i => i.Item2).ToArray());
        }

        private static void ExtractExclusionList(XElement doc, string nodeName, string fileName)
        {
            var data = ExportExclusionList(
                    doc.Descendants().FirstOrDefault(c => c.Name == nodeName));
            WriteFile(fileName, data);
        }

        private static void WriteFile(string name, object data)
        {
            var outpath = Path.Combine(OutputFolder, name);
            if (!Directory.Exists(Path.GetDirectoryName(outpath)))
                Directory.CreateDirectory(Path.GetDirectoryName(outpath)!);
            Console.WriteLine($"Writing: {outpath}");
            File.WriteAllText(outpath,
                JsonConvert.SerializeObject(data, new JsonSerializerSettings() {Formatting = Formatting.Indented}));
            
        }

        private static Dictionary<string, List<string>> ExportExclusionList(XElement n)
        {
            var outData = new Dictionary<string, List<string>>();
            
            outData["EDID"] = new List<string>();
            outData["NAME"] = new List<string>();
            outData["FORMID"] = new List<string>();
            
            foreach (var e in n.Descendants().Where(d => d.Name == "exclusion"))
            {
                ParseExclusion(e, out var text, out var target);
                outData[target].Add(text);
            }

            return outData;

        }

        private static void ParseExclusion(XElement e, out string text, out string target)
        {
            text = e.Descendants().First(d => d.Name == "text").Value;
            target = e.Descendants().First(d => d.Name == "target").Value;
            var type = e.Descendants().First(d => d.Name == "type").Value;

            text = Regex.Escape(text);

            switch (type)
            {
                case "CONTAINS":
                    break;
                case "STARTSWITH":
                    text = "^" + text;
                    break;
                case "ENDSWITH":
                    text += "$";
                    break;
                case "EQUALS":
                    text = "^" + text + "$";
                    break;
                default:
                    throw new NotImplementedException($"No operator {type}");
            }
        }
    }
}