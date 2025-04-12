#nullable enable
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.Shaders.HlslSupport.Daemon.CodeInsights;

[ShellComponent(Instantiation.DemandAnyThreadSafe)]
public class InactiveShaderVariantBranchCodeInsightsProvider : ICodeInsightsProvider, IInactiveShaderBranchHighlightFactory
{
    public string ProviderId => "Unity inactive shader variant branch";
    public string DisplayName => Strings.InactiveShaderVariantBranch_Text;
    public CodeVisionAnchorKind DefaultAnchor => CodeVisionAnchorKind.Top;
    public ICollection<CodeVisionRelativeOrdering> RelativeOrderings { get; }  = [new CodeVisionRelativeOrderingFirst()];
    public bool IsAvailableIn(ISolution solution) => true;

    public void OnClick(CodeInsightHighlightInfo highlightInfo, ISolution solution, CodeInsightsClickInfo clickInfo)
    {
        if (highlightInfo.CodeInsightsHighlighting is not InactiveShaderVariantBranchHighlight highlight)
        {
            Assertion.Fail($"InactiveShaderVariantBranchCodeInsightsProvider used with highlight different from InactiveShaderVariantBranchHighlight ({highlightInfo.CodeInsightsHighlighting.GetType()})");
            return;
        }
        
        solution.TryGetComponent<IShaderVariantsHost>()?.ShowShaderVariantInteraction(highlight.Range.StartOffset, ShaderVariantInteractionOrigin.CodeVision, highlight.ScopeKeywords);
    }

    public void OnExtraActionClick(CodeInsightHighlightInfo highlightInfo, string actionId, ISolution solution) => Assertion.Fail($"Extra action not implemented: {actionId}");

    public IHighlighting CreateInactiveShaderBranchHighlight(DocumentRange documentRange, ICollection<string> scopeKeywords) => new InactiveShaderVariantBranchHighlight(documentRange, DisplayName, this, scopeKeywords);
}