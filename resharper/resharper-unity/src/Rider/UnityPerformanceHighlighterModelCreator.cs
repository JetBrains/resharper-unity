using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.Daemon;
using JetBrains.ReSharper.Host.Features.Daemon.Registration;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.Rider.Model;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPerformanceHighlighterModelCreator : IRiderHighlighterModelCreator
    {
        private readonly IRiderHighlighterRegistry myRiderHighlighterRegistry;

        public UnityPerformanceHighlighterModelCreator(IRiderHighlighterRegistry riderHighlighterRegistry)
        {
            myRiderHighlighterRegistry = riderHighlighterRegistry;
        }
        
        public bool Accept(IHighlighter highlighter)
        {
            if (highlighter.AttributeId == null)
                return false;
            return highlighter.AttributeId.Equals(PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER);
        }

        public HighlighterModel CreateModel(long id, DocumentVersion documentVersion, IHighlighter highlighter, int shift)
        {
            var start = highlighter.Range.StartOffset + shift;
            var end = highlighter.Range.EndOffset + shift;
            return new UnityPerformanceHiglighterModel(myRiderHighlighterRegistry.GetLayer(highlighter), true, documentVersion, null,
                id, highlighter.AttributeId, start, end);
        }

        public int Priority => HighlighterModelCreatorPriorities.LINE_MARKERS;
    }
}