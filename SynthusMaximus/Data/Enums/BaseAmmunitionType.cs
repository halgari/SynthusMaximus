using System.Runtime.Serialization;

namespace SynthusMaximus.Data.Enums
{
    public enum BaseAmmunitionType
    {
        [EnumMember(Value = "ARROW")]
        Arrow,
        [EnumMember(Value = "BOLT")]
        Bolt,
    }
}