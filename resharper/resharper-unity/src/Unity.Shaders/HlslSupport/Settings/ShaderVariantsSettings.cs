#nullable enable
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Core.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Settings;

[SettingsKey(typeof(ShaderSettings), DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.ShaderVariantsSettings_t_Shader_variants_settings))]
public class ShaderVariantsSettings
{
    [SettingsIndexedEntry(DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.ShaderVariantsSettings_e_EnabledKeywords_t_Enabled_keywords))]
    public IIndexedEntry<string, bool> EnabledKeywords = null!;
}
