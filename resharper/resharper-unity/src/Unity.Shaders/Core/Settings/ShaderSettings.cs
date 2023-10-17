#nullable enable
using System.Reflection;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Core.Settings;

[SettingsKey(typeof(Missing), DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.ShaderSettings_t_Shader_settings))]
public class ShaderSettings
{
}