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
            AddEmptyLine();
            
            CheckBox((UnitySettings s) => s.EnableShaderLabParsing,
                "Parse ShaderLab files for syntax errors");
            
            AddEmptyLine();
            AddText("Disable this to avoid incorrect syntax error highlighting in .shader files.");
            AddText("The solution must be reopened when changed.");
            AddEmptyLine();
            AddText("Note that CGPROGRAM blocks are not currently checked for syntax errors.");

            if (productConfigurations.IsInternalMode())
            {
                CheckBox((UnitySettings s) => s.EnableCgErrorHighlighting, "Parse Cg files for syntax errors. Only works in internal mode.");
                AddText("Requires solution reopen, same as ShaderLab settings.");
            }
            
            FinishPage();
        }
    }
}