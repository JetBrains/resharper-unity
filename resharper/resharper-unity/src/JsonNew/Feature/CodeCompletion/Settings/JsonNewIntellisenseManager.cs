using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.CodeCompletion.Settings
{
    [ShellComponent]
    public class JsonNewIntellisenseManager : LanguageSpecificIntellisenseManager
    {
        public JsonNewIntellisenseManager([NotNull] ISettingsStore settingsStore)
            : base(settingsStore)
        {
        }

        public override SettingsScalarEntry GetSettingsEntry()
        {
            return SettingsStore.Schema.GetScalarEntry((IntellisenseEnabledSettingJsonNew setting) =>
                setting.IntellisenseEnabled);
        }

        public override PsiLanguageType PsiLanguage => JsonNewLanguage.Instance;
    }
}