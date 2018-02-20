using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.UI.Options;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [OptionsPage(
        PID,
        "Unity Engine",
        typeof(LogoThemedIcons.UnityLogo),
        Sequence = 0.01,
        ParentId = CodeEditingPage.PID)]
    public class UnityOptionsPage : OptionsPageBase
    {
        public const string PID = "UnityPluginSettings";

        public UnityOptionsPage(
            Lifetime lifetime,
            OptionsSettingsSmartContext optionsSettingsSmartContext,
            RunsProducts.ProductConfigurations productConfigurations)
            : base(lifetime, optionsSettingsSmartContext)
        {
            Header("General");

            CheckBox((UnitySettings s) => s.InstallUnity3DRiderPlugin, "Install or update Rider plugin automatically");

            Header("ShaderLab");

            CheckBox((UnitySettings s) => s.EnableShaderLabHippieCompletion, "Enable simple word-based completion in ShaderLab files");

            if (productConfigurations.IsInternalMode())
            {
                AddEmptyLine();
                CheckBox((UnitySettings s) => s.EnableCgErrorHighlighting, "Parse Cg files for syntax errors. Only works in internal mode.");
                AddText("Requires solution reopen.");
            }

            FinishPage();
        }
    }
}