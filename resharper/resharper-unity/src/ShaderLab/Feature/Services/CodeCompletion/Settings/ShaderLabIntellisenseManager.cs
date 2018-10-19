using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.CodeCompletion.Settings
{
    [ShellComponent]
    public class ShaderLabIntellisenseManager : LanguageSpecificIntellisenseManager
    {
        public ShaderLabIntellisenseManager([NotNull] ISettingsStore settingsStore)
            : base(settingsStore)
        {
        }

        public override SettingsScalarEntry GetSettingsEntry()
        {
            return SettingsStore.Schema.GetScalarEntry((IntellisenseEnabledSettingShaderLab setting) =>
                setting.IntellisenseEnabled);
        }

        public override PsiLanguageType PsiLanguage => ShaderLabLanguage.Instance;
    }
}