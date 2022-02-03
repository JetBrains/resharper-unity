using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Settings
{
    [SettingsKey(typeof(AutopopupEnabledSettingsKey), "ShaderLab")]
    public class ShaderLabAutopopupEnabledSettingsKey
    {
        [SettingsEntry(AutopopupType.HardAutopopup, "In variable references")]
        public AutopopupType InVariableReferences;
    }
}