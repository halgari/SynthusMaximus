using SynthusMaximus.Data.DTOs;
using Wabbajack.Common;

namespace SynthusMaximus.Data.Enums
{
    public class MaterialEnum : DynamicEnum<Material>
    {
        public MaterialEnum(OverlayLoader loader) : base((RelativePath)@"materials.json", loader)
        {
        }
    }
}