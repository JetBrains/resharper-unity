using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    public class PerformanceHighlightingConsumer : DefaultHighlightingConsumer
    {
        private readonly IgnoreWarningsRegionsInfo myRegionsInfo;

        public PerformanceHighlightingConsumer([NotNull] IPsiSourceFile sourceFile, [NotNull] IFile psiFile)
            : base(sourceFile)
        {
            var solution = psiFile.GetSolution();
            myRegionsInfo = solution.GetComponent<IRegionsInfoProvider>().GetOrCreateInfoForDocument(SourceFile.Document) as IgnoreWarningsRegionsInfo;
        }

        public override void ConsumeHighlighting(HighlightingInfo highlightingInfo)
        {
            var highlighting = highlightingInfo.Highlighting;
            Assertion.Assert(highlighting is PerformanceHighlightingBase, "highlightingInfo is PerformanceCriticalCodeHighlightingBase");

            var performanceHighlighting = highlighting as PerformanceHighlightingBase;
            if (myRegionsInfo != null)
            {
                if (myRegionsInfo.ShouldIgnoreWarningHighlighting(performanceHighlighting.SeverityId, highlightingInfo, false, out var _))
                    return;
            }

            ConsumeInfo(highlightingInfo);
        }
    }
}