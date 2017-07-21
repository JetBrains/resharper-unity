#if RIDER
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
    public class UnityPluginOptionsPage : OptionsPageBase
    {
        public const string PID = "UnityPluginSettings";
        
        public UnityPluginOptionsPage(Lifetime lifetime, OptionsSettingsSmartContext optionsSettingsSmartContext)
            : base(lifetime, optionsSettingsSmartContext)
        {
            Header("General");
            
            CheckBox((UnityPluginSettings s) => s.InstallUnity3DRiderPlugin, "Install or update Rider plugin automatically");
            
            Header("ShaderLab");

            CheckBox((UnityPluginSettings s) => s.EnableShaderLabParsing, "Parse ShaderLab files for syntax errors");
            AddEmptyLine();
            AddText("Disable this to avoid incorrect syntax error highlighting in .shader files.");
            AddText("The solution must be reopened when changed.");
            AddEmptyLine();
            AddText("Note that CGPROGRAM blocks are not currently checked for syntax errors.");

            FinishPage();
        }
    }
}
#endif