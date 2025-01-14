using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityProfilerInsightProvider(IFrontendBackendHost frontendBackendHost, ILazy<BulbMenuComponent> bulbMenu)
    : AbstractUnityCodeInsightProvider(frontendBackendHost, bulbMenu)
{
    public override string ProviderId => "Unity profiler";
    public override string DisplayName => Strings.UnityProfilerSnapshot_Text ;
    public override CodeVisionAnchorKind DefaultAnchor => CodeVisionAnchorKind.Right;

    public override ICollection<CodeVisionRelativeOrdering> RelativeOrderings =>
        new List<CodeVisionRelativeOrdering> { new CodeVisionRelativeOrderingLast() };
}