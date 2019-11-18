using System;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [DaemonStage(GlobalAnalysisStage = true)]
    public class PerformanceCriticalCodeAnalysisStage : CSharpDaemonStageBase
    {
        protected readonly SolutionAnalysisService Swa;
        private readonly UnitySolutionTracker mySolutionTracker;
        protected readonly CallGraphActivityTracker Tracker;
        protected readonly CallGraphSwaExtensionProvider CallGraphSwaExtension;
        protected readonly PerformanceCriticalCodeCallGraphAnalyzer PerformanceAnalyzer;
        protected readonly ExpensiveCodeCallGraphAnalyzer ExpensiveAnalyzer;

        public PerformanceCriticalCodeAnalysisStage(SolutionAnalysisService swa,
            UnitySolutionTracker solutionTracker, CallGraphActivityTracker tracker,
            CallGraphSwaExtensionProvider callGraphSwaExtension,
            PerformanceCriticalCodeCallGraphAnalyzer performanceAnalyzer,
            ExpensiveCodeCallGraphAnalyzer expensiveAnalyzer)
        {
            Swa = swa;
            mySolutionTracker = solutionTracker;
            Tracker = tracker;
            CallGraphSwaExtension = callGraphSwaExtension;
            PerformanceAnalyzer = performanceAnalyzer;
            ExpensiveAnalyzer = expensiveAnalyzer;
        }
        
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (!file.GetProject().IsUnityProject())
                return null;
            
            var enabled = settings.GetValue((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);

            if (!enabled)
                return null;

            return GetProcess(process, settings, processKind, file);
        }

        protected virtual IDaemonStageProcess GetProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            return new PerformanceCriticalCodeAnalysisProcess(process, file, CallGraphSwaExtension, Swa, Tracker, PerformanceAnalyzer, ExpensiveAnalyzer);
        }
        
        protected override bool IsSupported(IPsiSourceFile sourceFile)
        {
            if (sourceFile == null || !sourceFile.IsValid())
                return false;

            return sourceFile.IsLanguageSupported<CSharpLanguage>();
        }
    }

    internal class PerformanceCriticalCodeAnalysisProcess : CSharpDaemonStageProcessBase
    {
        private readonly CallGraphSwaExtensionProvider myCallGraphSwaExtensionProvider;
        [NotNull] private readonly SolutionAnalysisService mySwa;
        private readonly CallGraphActivityTracker myTracker;
        private readonly CallGraphAnalyzerId myPerformanceAnalyzerId;
        private readonly CallGraphAnalyzerId myExpensiveAnalyzerId;
        private readonly bool myLineMarkerEnabled;

        public PerformanceCriticalCodeAnalysisProcess([NotNull] IDaemonProcess process, [NotNull] ICSharpFile file, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            [NotNull] SolutionAnalysisService swa, CallGraphActivityTracker tracker, [NotNull] PerformanceCriticalCodeCallGraphAnalyzer performanceAnalyzer,
            [NotNull] ExpensiveCodeCallGraphAnalyzer expensiveCodeCallGraphAnalyzer)
            : base(process, file)
        {
            myCallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            mySwa = swa;
            myTracker = tracker;
            myPerformanceAnalyzerId = performanceAnalyzer.Id;
            myExpensiveAnalyzerId = expensiveCodeCallGraphAnalyzer.Id;

            myLineMarkerEnabled = DaemonProcess.ContextBoundSettingsStore.GetValue((UnitySettings key) =>
                key.PerformanceHighlightingMode) == PerformanceHighlightingMode.Always;
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            HighlightInFile(AnalyzeFile, committer, DaemonProcess.ContextBoundSettingsStore);
        }

        private void AnalyzeFile(ICSharpFile file, IHighlightingConsumer consumer)
        {
            if (!file.GetProject().IsUnityProject())
                return;

            var sourceFile = file.GetSourceFile();
            if (sourceFile == null)
                return;

            file.ProcessThisAndDescendants(this, consumer);
        }

        public override void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            var node = element as ICSharpTreeNode;
            if (node == null)
                return;
            
            var usageChecker = mySwa.UsageChecker;
            if (usageChecker == null)
                return;
            
            if (node is IFunctionDeclaration functionDeclaration)
            {
                var declaredElement = functionDeclaration.DeclaredElement;
                if (myLineMarkerEnabled && IsMarked(usageChecker, myPerformanceAnalyzerId, declaredElement))
                    HighlightHotMethod(functionDeclaration, consumer);               
            }


            if (IsMarked(usageChecker, myPerformanceAnalyzerId,
                node.GetContainingFunctionLikeDeclarationOrClosure()?.DeclaredElement))
            {
                var higlighting = PerformanceCriticalCodeStageUtil.CreateHiglighting(node);
                if (higlighting != null)
                {
                    consumer.AddHighlighting(higlighting);
                }
                else
                {
                    if (node is IInvocationExpression expression)
                    {
                        if (IsMarked(usageChecker, myExpensiveAnalyzerId,
                            expression.Reference?.Resolve().DeclaredElement))
                        {
                            consumer.AddHighlighting(new PerformanceInvocationHighlighting(expression,
                                (expression.InvokedExpression as IReferenceExpression)?.Reference));
                        }
                    }
                }

                base.ProcessBeforeInterior(node, consumer);
            }
        }


        private bool IsMarked(IGlobalUsageChecker usageChecker, CallGraphAnalyzerId analyzerId,  IDeclaredElement element)
        {
            if (element == null)
                return false;
            var id = mySwa.GetElementId(element);
            
            if (!id.HasValue)
                return false;

            return myTracker.RegisterCallGraphQueryTime(() =>
                myCallGraphSwaExtensionProvider.IsMarkedByCallGraphAnalyzer(analyzerId, id.Value, true));
        }

        protected virtual void HighlightHotMethod(IDeclaration node, IHighlightingConsumer consumer)
        {
            var range = node.GetDocumentRange();
            consumer.AddHighlighting(new PerformanceHighlighting(range));
        }
    }
}