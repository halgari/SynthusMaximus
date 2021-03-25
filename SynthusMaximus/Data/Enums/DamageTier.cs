using System.Runtime.Serialization;

namespace SynthusMaximus.Data.Enums
{
    public enum DamageTier : int
    {
        [EnumMember(Value = "Zero")]
        Zero,
        [EnumMember(Value = "ONE")]
        One,
        [EnumMember(Value = "TWO")]
        Two,
        [EnumMember(Value = "THREE")]
        Three,
        [EnumMember(Value = "FOUR")]
        Four,
        [EnumMember(Value = "FIVE")]
        Five
    }
}