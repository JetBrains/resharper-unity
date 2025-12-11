#nullable enable
using System;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features.Daemon;
using JetBrains.RdBackend.Common.Features.Daemon.Registration;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights;

[SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
public class UnityProfilerHighlighterModelCreator(IRiderHighlighterRegistry registry) : IRiderHighlighterModelCreator
{
    private IRiderHighlighterRegistry RiderHighlighterRegistry { get; } = registry;

    public bool Accept(IHighlighter highlighter)
    {
        return highlighter.GetHighlighting<UnityProfilerHighlighting>() != null;
    }

    public int Priority => HighlighterModelCreatorPriorities.GUTTER_MARKS;

    public HighlighterModel CreateModel(long id, AbstractDocumentVersion documentVersion, IHighlighter highlighter,
        int shift)
    {
        var highlightingInfo = highlighter.GetHighlighting<UnityProfilerHighlighting>();

        if (highlightingInfo == null)
            throw new ArgumentException("Unknown highlighter type", nameof(highlighter));

        var highlighterProperties = RiderHighlighterRegistry.GetPropertiesOrDefault(highlighter.AttributeId);
        var textAttributesKeyModel = RiderHighlighterRegistry.GetTextAttributesKeyModel(highlighter.AttributeId);

        int start = highlighter.Range.StartOffset + shift;
        int end = highlighter.Range.EndOffset + shift;
        return new ProfilerHighlighterModel(
            highlightingInfo.SampleInfo,
            RiderHighlighterRegistry.GetLayer(highlighter),
            true,
            documentVersion, 
            false,
            false, 
            false,
            null,
            textAttributesKeyModel,
            id,
            highlighterProperties, 
            start,
            end
        );
    }
}