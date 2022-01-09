using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.CodeCompletion.Settings
{
    [SettingsKey(typeof(IntellisenseEnabledSettingsKey), "Override VS IntelliSense for ShaderLab")]
    public class IntellisenseEnabledSettingShaderLab
    {
        [SettingsEntry(false, "ShaderLab (Unity .shader files)")]
        public bool IntellisenseEnabled;
    }
}