using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.Shaders.HlslSupport.Daemon.CodeInsights;

[StaticSeverityHighlighting(Severity.INFO, typeof(ShaderKeywordsHighlightingId), OverlapResolve = OverlapResolveKind.NONE,
    AttributeId = ShaderLabHighlightingAttributeIds.INACTIVE_SHADER_VARIANT_BRANCH)]
public class InactiveShaderVariantBranchHighlight(DocumentRange branchRange, /*Localized*/ string displayName, ICodeInsightsProvider provider, ICollection<string> scopeKeywords)
    : CodeInsightsHighlighting(branchRange, displayName, Strings.ThisBranchMayBeActiveInOneOfShaderVariants_Text, Strings.ConfigureShaderVariantKeywords_Text, provider, null, null), IUnityIndicatorHighlighting
{
    public ICollection<string> ScopeKeywords => scopeKeywords;
}