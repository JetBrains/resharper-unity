#nullable enable
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Core.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Settings;

[SettingsKey(typeof(ShaderSettings), DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.ShaderVariantsSettings_t_Shader_variants_settings))]
public class ShaderVariantsSettings
{
    [SettingsEntry(ShaderApi.D3D11, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.ShaderVariantsSettings_e_ShaderApi_t_Shader_API))]
    public ShaderApi ShaderApi = ShaderApi.D3D11;
    
    [SettingsIndexedEntry(DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.ShaderVariantsSettings_e_EnabledKeywords_t_Enabled_keywords))]
    public IIndexedEntry<string, bool> EnabledKeywords = null!;
}
