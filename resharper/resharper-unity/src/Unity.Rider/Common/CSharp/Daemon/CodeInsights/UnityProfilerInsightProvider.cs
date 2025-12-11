using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
[HighlightingSource(HighlightingTypes = [typeof(UnityProfilerHighlighting)])]
public class UnityProfilerInsightProvider(IFrontendBackendHost frontendBackendHost, ILazy<BulbMenuComponent> bulbMenu)
    : AbstractUnityCodeInsightProvider(frontendBackendHost, bulbMenu)
{
    public override string ProviderId => "Unity profiler";
    public override string DisplayName => Strings.UnityProfilerSnapshot_Text ;
    public override CodeVisionAnchorKind DefaultAnchor => CodeVisionAnchorKind.Right;

    public override ICollection<CodeVisionRelativeOrdering> RelativeOrderings => new List<CodeVisionRelativeOrdering> { new CodeVisionRelativeOrderingLast() };

    public void AddProfilerHighlighting(ModelUnityProfilerSampleInfo sampleInfo,
        FilteringHighlightingConsumer consumer,
        DocumentRange documentRange, IDeclaredElement declaredElement, string displayName, string tooltip,
        string moreText,
        IconModel iconModel, IEnumerable<BulbMenuItem> items, List<CodeVisionEntryExtraActionModel> extraActions)
    {
       AddHighlighting(consumer, documentRange, declaredElement, displayName, tooltip, moreText, iconModel, items, extraActions);
       if(sampleInfo.RenderSettings == ProfilerGutterMarkRenderSettings.Hidden)
           return;
       
       
       //adds new custom gutter mark
       var highlighting = new UnityProfilerHighlighting(documentRange, sampleInfo);
       consumer.AddHighlighting(highlighting);
    }
}