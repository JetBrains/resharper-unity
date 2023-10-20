#nullable enable
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;
using JetBrains.ReSharper.Resources.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Core.Settings;

[SettingsKey(typeof(CodeEditingSettings), DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.ShaderSettings_t_Shader_settings))]
public class ShaderSettings
{
}