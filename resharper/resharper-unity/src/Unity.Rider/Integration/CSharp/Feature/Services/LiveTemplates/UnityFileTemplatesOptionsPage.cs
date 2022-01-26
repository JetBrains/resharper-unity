using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.IDE.UI;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Settings;
using JetBrains.ReSharper.LiveTemplates.UI;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.CSharp.Feature.Services.LiveTemplates
{
    [ZoneMarker(typeof(IRiderModelZone))]
    [OptionsPage("RiderUnityFileTemplatesSettings", "Unity", typeof(LogoIcons.Unity))]
    public class UnityFileTemplatesOptionsPage : RiderFileTemplatesOptionPageBase
    {
        public UnityFileTemplatesOptionsPage(Lifetime lifetime,
                                             UnityProjectScopeCategoryUIProvider uiProvider,
                                             OptionsPageContext optionsPageContext,
                                             OptionsSettingsSmartContext optionsSettingsSmartContext,
                                             StoredTemplatesProvider storedTemplatesProvider,
                                             ScopeCategoryManager scopeCategoryManager,
                                             IDialogHost dialogHost,
                                             TemplatesUIFactory uiFactory, IconHostBase iconHostBase)
            : base(lifetime, uiProvider, optionsPageContext, optionsSettingsSmartContext, storedTemplatesProvider, scopeCategoryManager,
                uiFactory, iconHostBase, dialogHost, "CSHARP")
        {
        }
    }
}