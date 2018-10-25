using System;
using JetBrains.Application;
using JetBrains.ReSharper.Host.Features.Daemon;
using JetBrains.ReSharper.Host.Features.Daemon.Registration;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.Rider.Model.HighlighterRegistration;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [ShellComponent]
    public class PerformanceHighlighterPropertiesProvider : IRiderHighlighterPropertiesProvider
    {
        public bool Applicable(RiderHighlighterDescription description)
        {
            return description.AttributeId.Equals(PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER);
        }

        public HighlighterProperties GetProperties(RiderHighlighterDescription description)
        {
            var highlighterKind = description.Kind.ToModel();
            return new HighlighterProperties(highlighterKind, true, GreedySide.NONE, false, false, false);
        }

        public int Priority => 1;
    }
}