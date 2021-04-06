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
            ExtractExclusionList(leveledLists, "distribution_exclusions_list_regular", @"exclusions\distributionExclusionsWeaponListRegular.json");
            ExtractExclusionList(leveledLists, "distribution_exclusions_spell", @"exclusions\distributionExclusionsSpell.json");
            
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

            var npcs = XElement.Load("NPC.xml");
            ExtractExclusionList(npcs, "npc_exclusions", @"exclusions\npcs.json");
            ExtractExclusionList(npcs, "race_exclusions", @"exclusions\race.json");

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
                var text = e.Descendants().First(d => d.Name == "text").Value;
                var target = e.Descendants().First(d => d.Name == "target").Value;
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

                
                outData[target].Add(text);

            }

            return outData;

        }
    }
}