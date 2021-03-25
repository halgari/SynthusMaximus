using SynthusMaximus.Data.DTOs.Weapon;
using Wabbajack.Common;

namespace SynthusMaximus.Data.Enums
{
    public class BaseWeaponTypeEnum  : DynamicEnum<BaseWeaponType>
    {
        public BaseWeaponTypeEnum(OverlayLoader loader) : base((RelativePath)@"weapons\baseWeaponTypes.json", loader)
        {
        }
    }
}