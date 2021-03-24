using System.Runtime.Serialization;

namespace SynthusMaximus.Data.Enums
{
    public enum ArmorClass
    {
        [EnumMember(Value = "UNDEFINED")]
        Undefined,
        [EnumMember(Value = "LIGHT")]
        Light,
        [EnumMember(Value = "HEAVY")]
        Heavy,
        [EnumMember(Value = "BOTH")]
        Both,
    }
}