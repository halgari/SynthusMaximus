using SynthusMaximus.Data.DTOs.Weapon;
using Wabbajack.Common;

namespace SynthusMaximus.Data.Enums
{
    public class WeaponClassEnum : DynamicEnum<WeaponClass>
    {
        public WeaponClassEnum(OverlayLoader loader) : base((RelativePath)@"weapons\weaponClasses.json", loader)
        {
        }
    }
}