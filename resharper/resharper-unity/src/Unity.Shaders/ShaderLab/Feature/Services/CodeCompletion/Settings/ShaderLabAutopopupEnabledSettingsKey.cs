using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Settings
{
    [SettingsKey(typeof(AutopopupEnabledSettingsKey), DescriptionResourceType: typeof (Strings), DescriptionResourceName:nameof(Strings.ShaderLabAutopopupEnabledSettingsKey_s_ShaderLab))]
    public class ShaderLabAutopopupEnabledSettingsKey
    {
        [SettingsEntry(AutopopupType.HardAutopopup, DescriptionResourceType: typeof(Strings), DescriptionResourceName:nameof(Strings.ShaderLabAutopopupEnabledSettingsKey_s_In_variable_references))]
        public AutopopupType InVariableReferences;
        
        [SettingsEntry(AutopopupType.HardAutopopup, DescriptionResourceType: typeof(Strings), DescriptionResourceName:nameof(Strings.ShaderLabAutopopupEnabledSettingsKey_s_In_keywords))]
        public AutopopupType InKeywords;
        
        [SettingsEntry(AutopopupType.HardAutopopup, DescriptionResourceType: typeof(Strings), DescriptionResourceName:nameof(Strings.ShaderLabAutopopupEnabledSettingsKey_s_In_shader_references))]
        public AutopopupType InShaderReferences;
        
        [SettingsEntry(AutopopupType.HardAutopopup, DescriptionResourceType: typeof(Strings), DescriptionResourceName:nameof(Strings.ShaderLabAutopopupEnabledSettingsKey_s_In_pass_references))]
        public AutopopupType InPassReferences;
    }
}