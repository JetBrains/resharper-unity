using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Options
{
  [SolutionComponent(InstantiationEx.LegacyDefault)]
  public class UnityHintsOptionsRegistrar
  {
    public UnityHintsOptionsRegistrar(InlayHintsOptionsStore inlayHintsOptionsStore,
      ISettingsStore settingsStore)
    {
      inlayHintsOptionsStore.RegisterSettingsKeyToRehighlightVisibleDocumentOnItsChange(settingsStore.Schema
        .GetKey<UnityInlayHintSettings>());
    }
  }
}