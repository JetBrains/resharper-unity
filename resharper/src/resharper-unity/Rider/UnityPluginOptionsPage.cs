#if RIDER
using JetBrains.Application.UI.Options;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;
using JetBrains.ReSharper.Plugins.Unity.Resources;

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

            FinishPage();
        }
    }
}
#endif