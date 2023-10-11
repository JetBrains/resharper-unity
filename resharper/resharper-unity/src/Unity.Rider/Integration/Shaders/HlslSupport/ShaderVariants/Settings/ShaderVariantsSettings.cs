#nullable enable
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants.Settings;

[SettingsKey(typeof(UnityProjectSettings), DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.ShaderVariantsSettings_t_Shader_variants_settings))]
public class ShaderVariantsSettings
{
    [SettingsIndexedEntry(DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.ShaderVariantsSettings_e_SelectedVariants_t_Selected_variants))]
    public IIndexedEntry<string, string> SelectedVariants = null!;
}
