using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Settings
{
    [SettingsKey(typeof(IntellisenseEnabledSettingsKey), DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.IntellisenseEnabledSettingShaderLab_s_Override_VS_IntelliSense_for_ShaderLab))]
    public class IntellisenseEnabledSettingShaderLab
    {
        [SettingsEntry(false, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.IntellisenseEnabledSettingShaderLab_s_ShaderLab__Unity__shader_files_))]
        public bool IntellisenseEnabled;
    }
}