#nullable enable
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Settings;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.CodeCompletion
{
    [ShellComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class UxmlCodeCompletionManager : LanguageSpecificCodeCompletionManager
    {
        public UxmlCodeCompletionManager(CodeCompletionSettingsService codeCompletionSettings)
            : base(codeCompletionSettings) { }

        public override SettingsScalarEntry GetSettingsEntry(ISettingsSchema settingsSchema)
        {
            return settingsSchema.GetScalarEntry((IntellisenseEnabledSettingCSharp setting) => setting.IntellisenseEnabled);
        }

        public override PsiLanguageType PsiLanguage => UxmlLanguage.Instance.NotNull();
    }
}

