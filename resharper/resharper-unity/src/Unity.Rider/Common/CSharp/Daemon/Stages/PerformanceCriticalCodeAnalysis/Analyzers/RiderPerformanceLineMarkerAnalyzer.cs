using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent(Instantiation.ContainerAsyncAnyThread)]
    public class RiderPerformanceLineMarkerAnalyzer : PerformanceLineMarkerAnalyzer
    {
        public RiderPerformanceLineMarkerAnalyzer(Lifetime lifetime, IApplicationWideContextBoundSettingStore store, IThreading threading)
            : base(lifetime, store, threading)
        {
        }

        protected override DocumentRange GetHighlightRange(IFunctionDeclaration functionDeclaration)
        {
            // Rider supports line markers (unlike ReSharper), so highlight the entire function
            return functionDeclaration.GetDocumentRange();
        }
    }
}