using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class RiderPerformanceLineMarkerAnalyzer(Lifetime lifetime, ISettingsStore settingsStore)
        : PerformanceLineMarkerAnalyzer(lifetime, settingsStore)
    {
        protected override DocumentRange GetHighlightRange(IFunctionDeclaration functionDeclaration)
        {
            // Rider supports line markers (unlike ReSharper), so highlight the entire function
            return functionDeclaration.GetDocumentRange();
        }
    }
}