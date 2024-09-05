using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion.Settings
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class ShaderLabIntellisenseManager : LanguageSpecificCodeCompletionManager
    {
        public ShaderLabIntellisenseManager(CodeCompletionSettingsService settingsService)
            : base(settingsService)
        {
        }

        public override SettingsScalarEntry GetSettingsEntry(ISettingsSchema settingsSchema)
        {
            return settingsSchema.GetScalarEntry((IntellisenseEnabledSettingShaderLab setting) =>
                setting.IntellisenseEnabled);
        }

        public override PsiLanguageType PsiLanguage => ShaderLabLanguage.Instance;
    }
}