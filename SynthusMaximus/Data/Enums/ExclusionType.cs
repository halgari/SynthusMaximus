using System.Runtime.Serialization;

namespace SynthusMaximus.Data.Enums
{
    public enum ExclusionType : int
    {
        [EnumMember(Value = "NAME")]
        Name,
        [EnumMember(Value = "EDID")]
        EDID,
        [EnumMember(Value = "FULL")]
        Full,
        [EnumMember(Value = "FORMID")]
        FormID
        
        
    }
}