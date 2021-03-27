using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Xml2Json
{
    class Program
    {
        static void Main(string[] args)
        {
            var doc = XElement.Load("LeveledLists.xml");

            ExtractExclusionList(doc, "distribution_exclusions_weapon_regular", "distributionExclusionsWeaponRegular.json");
            ExtractExclusionList(doc, "distribution_exclusions_list_regular", "distributionExclusionsWeaponListRegular.json");
            
            
        }

        private static void ExtractExclusionList(XElement doc, string nodeName, string fileName)
        {
            var data = ExportExclusionList(
                    doc.Descendants().FirstOrDefault(c => c.Name == nodeName));
            File.WriteAllText(fileName,
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