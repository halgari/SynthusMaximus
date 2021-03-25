using SynthusMaximus.Data.DTOs.Weapon;
using Wabbajack.Common;

namespace SynthusMaximus.Data.Enums
{
    public class WeaponTypeEnum : DynamicEnum<WeaponType>
    {
        public WeaponTypeEnum(OverlayLoader loader) : base((RelativePath)@"weapons\weaponTypes.json", loader)
        {
        }
    }
}