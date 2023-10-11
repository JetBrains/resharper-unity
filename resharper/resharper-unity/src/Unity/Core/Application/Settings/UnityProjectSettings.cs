#nullable enable
using System.Reflection;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;

[SettingsKey(typeof(Missing), DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityProjectSettings_t_Unity_project_settings))]
public class UnityProjectSettings
{
}