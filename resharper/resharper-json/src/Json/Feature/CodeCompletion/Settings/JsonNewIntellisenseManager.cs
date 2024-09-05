using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Json.Feature.CodeCompletion.Settings
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class JsonNewIntellisenseManager : LanguageSpecificCodeCompletionManager
    {
        public JsonNewIntellisenseManager(CodeCompletionSettingsService settingsService)
            : base(settingsService)
        {
        }

        public override SettingsScalarEntry GetSettingsEntry(ISettingsSchema settingsSchema)
        {
            return settingsSchema.GetScalarEntry((IntellisenseEnabledSettingJsonNew setting) =>
                setting.IntellisenseEnabled);
        }

        public override PsiLanguageType PsiLanguage => JsonNewLanguage.Instance;
    }
}