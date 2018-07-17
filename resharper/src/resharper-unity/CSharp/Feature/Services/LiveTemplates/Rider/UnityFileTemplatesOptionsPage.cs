using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.UI.Options;
using JetBrains.DataFlow;
using JetBrains.IDE.UI;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Settings;
using JetBrains.ReSharper.LiveTemplates.UI;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Rider
{
    [ZoneMarker(typeof(IRiderModelZone))]
    [OptionsPage("RiderUnityFileTemplatesSettings", "Unity", typeof(LogoThemedIcons.UnityLogo))]
    public class UnityFileTemplatesOptionsPage : RiderFileTemplatesOptionPageBase
    {
        public UnityFileTemplatesOptionsPage(Lifetime lifetime,
                                             OptionsSettingsSmartContext optionsSettingsSmartContext,
                                             StoredTemplatesProvider storedTemplatesProvider,
                                             UnityProjectScopeCategoryUIProvider uiProvider,
                                             ScopeCategoryManager scopeCategoryManager,
                                             TemplatesUIFactory uiFactory, IconHostBase iconHostBase)
            : base(lifetime, uiProvider, optionsSettingsSmartContext, storedTemplatesProvider, scopeCategoryManager,
                uiFactory, iconHostBase, "CSHARP")
        {
        }
    }
}