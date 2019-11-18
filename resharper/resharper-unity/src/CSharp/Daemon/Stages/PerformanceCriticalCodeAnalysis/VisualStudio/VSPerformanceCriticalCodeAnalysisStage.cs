using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.VisualStudio
{
    // ReSharper disable once InconsistentNaming
    [DaemonStage(GlobalAnalysisStage = true)]
    public class VSPerformanceCriticalCodeAnalysisStage : PerformanceCriticalCodeAnalysisStage
    {
        public VSPerformanceCriticalCodeAnalysisStage(SolutionAnalysisService swa, UnitySolutionTracker solutionTracker, CallGraphActivityTracker tracker, PerformanceCriticalCodeCallGraphAnalyzer performanceAnalyzer,
            ExpensiveCodeCallGraphAnalyzer expensiveAnalyzer, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider)
            : base(swa, solutionTracker, tracker, callGraphSwaExtensionProvider, performanceAnalyzer, expensiveAnalyzer)
        {
        }
        
        protected override IDaemonStageProcess GetProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            return new VSPerformanceCriticalCodeAnalysisProcess(process, file, CallGraphSwaExtension, Swa,  Tracker, PerformanceAnalyzer, ExpensiveAnalyzer);
        }
    }

    // ReSharper disable once InconsistentNaming
    internal class VSPerformanceCriticalCodeAnalysisProcess : PerformanceCriticalCodeAnalysisProcess
    {
        public VSPerformanceCriticalCodeAnalysisProcess([NotNull] IDaemonProcess process, [NotNull] ICSharpFile file, CallGraphSwaExtensionProvider swaExtensionProvider,
            [NotNull] SolutionAnalysisService swa, [NotNull] CallGraphActivityTracker tracker, [NotNull]PerformanceCriticalCodeCallGraphAnalyzer performanceAnalyzer,
            ExpensiveCodeCallGraphAnalyzer expensiveAnalyzer)
            : base(process, file, swaExtensionProvider, swa, tracker, performanceAnalyzer, expensiveAnalyzer)
        {
        }

        protected override void HighlightHotMethod(IDeclaration node, IHighlightingConsumer consumer)
        {
            // ReSharper doesn't support LINE_MARKER highlightings, so we use an underline effect instead. Remove this
            // once ReSharper supports LINE_MARKER. See PerformanceCriticalCodeHighlighters.cs
            
            var range = node.GetNameDocumentRange();
            consumer.AddHighlighting(new PerformanceHighlighting(range));
        }
    }
}