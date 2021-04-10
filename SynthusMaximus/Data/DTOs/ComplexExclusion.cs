using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SynthusMaximus.Data.Enums;

namespace SynthusMaximus.Data.DTOs
{
    public class ComplexExclusion
    {
        [JsonProperty("targetA")] public ExclusionType TargetA { get; set; }
        [JsonProperty("textA")] public Regex TextA { get; set; } = new("");
        [JsonProperty("targetB")] public ExclusionType TargetB { get; set; }
        [JsonProperty("textA")] public Regex TextB { get; set; } = new("");
    }
}