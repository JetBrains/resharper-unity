using System;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
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
        protected readonly CallGraphActivityTracker Tracker;

        public PerformanceCriticalCodeAnalysisStage(SolutionAnalysisService swa, CallGraphActivityTracker tracker)
        {
            Swa = swa;
            Tracker = tracker;
        }
        
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            var enabled = settings.GetValue((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);

            if (!enabled)
                return null;

            return GetProcess(process, settings, processKind, file);
        }

        protected virtual IDaemonStageProcess GetProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            return new PerformanceCriticalCodeAnalysisProcess(process, file, Swa, Tracker);
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
        [NotNull] private readonly SolutionAnalysisService mySwa;
        private readonly CallGraphActivityTracker myTracker;
        private readonly int myPerformanceAnalyzerId;
        private readonly int myExpensiveAnalyzerId;

        public PerformanceCriticalCodeAnalysisProcess([NotNull] IDaemonProcess process, [NotNull] ICSharpFile file,
            [NotNull] SolutionAnalysisService swa, CallGraphActivityTracker tracker)
            : base(process, file)
        {
            mySwa = swa;
            myTracker = tracker;
            myPerformanceAnalyzerId = CallGraphAnalyzerIdToIntConverter.GetId(PerformanceCriticalCodeCallGraphAnalyzer.MarkId);
            myExpensiveAnalyzerId = CallGraphAnalyzerIdToIntConverter.GetId(ExpensiveCodeCallGraphAnalyzer.MarkId);
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            var highlightingConsumer = new FilteringHighlightingConsumer(new PerformanceHighlightingConsumer(DaemonProcess.SourceFile, File)
                ,DaemonProcess.SourceFile, File, DaemonProcess.ContextBoundSettingsStore);
            AnalyzeFile(File, highlightingConsumer);
            committer(new DaemonStageResult(highlightingConsumer.Highlightings));
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
                if (IsMarked(usageChecker, myPerformanceAnalyzerId, declaredElement))
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


        private bool IsMarked(IGlobalUsageChecker usageChecker, int analyzerId,  IDeclaredElement element)
        {
            if (element == null)
                return false;
            var id = mySwa.GetElementId(element, true);
            
            if (!id.HasValue)
                return false;
            
            return myTracker.RegisterCallGraphQueryTime(() =>
                usageChecker.IsMarked(analyzerId, id.Value, true));
        }

        protected virtual void HighlightHotMethod(IDeclaration node, IHighlightingConsumer consumer)
        {
            var range = node.GetDocumentRange();
            consumer.AddHighlighting(new PerformanceHighlighting(range));
        }
    }
}