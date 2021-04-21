using Mutagen.Bethesda.Synthesis.Settings;

namespace EnchantmentBindingGenerator
{
    public record Settings
    {
        [SynthesisOrder]
        [SynthesisSettingName("Mod to Analyze")]
        [SynthesisDescription("The patcher will analyze this mod and attempt to create listEnchantmentBindings.json from the file")]
        public string ExportMod = "Summermyst - Enchantments of Skyrim.esp";

    }
}