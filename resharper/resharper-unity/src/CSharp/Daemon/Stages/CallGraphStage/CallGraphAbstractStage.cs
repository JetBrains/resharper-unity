using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Collections;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    public abstract class CallGraphAbstractStage : CSharpDaemonStageBase
    {
        private readonly IEnumerable<ICallGraphContextProvider> myContextProviders;
        private readonly IEnumerable<ICallGraphProblemAnalyzer> myProblemAnalyzers;
        private readonly ILogger myLogger;

        protected CallGraphAbstractStage(
            IEnumerable<ICallGraphContextProvider> contextProviders,
            IEnumerable<ICallGraphProblemAnalyzer> problemAnalyzers,
            ILogger logger)
        {
            myContextProviders = contextProviders;
            myProblemAnalyzers = problemAnalyzers;
            myLogger = logger;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (!file.GetProject().IsUnityProject())
                return null;

            return new CallGraphProcess(process, processKind, file, myLogger, myContextProviders, myProblemAnalyzers);
        }
    }

    public class CallGraphProcess : CSharpDaemonStageProcessBase
    {
        private readonly DaemonProcessKind myProcessKind;
        private readonly ILogger myLogger;
        private readonly IEnumerable<ICallGraphContextProvider> myContextProviders;
        private readonly Dictionary<CallGraphContextElement,List<ICallGraphProblemAnalyzer>> myProblemAnalyzersByContext;
        private readonly CallGraphContext myContext = new CallGraphContext();

        public CallGraphProcess(
            IDaemonProcess process,
            DaemonProcessKind processKind, 
            ICSharpFile file, 
            ILogger logger,
            IEnumerable<ICallGraphContextProvider> contextProviders,
            IEnumerable<ICallGraphProblemAnalyzer> problemAnalyzers)
            : base(process, file)
        {
            myProcessKind = processKind;
            myLogger = logger;
            myContextProviders = contextProviders;

            myProblemAnalyzersByContext = problemAnalyzers.GroupBy(t => t.Context)
                .ToDictionary(t => t.Key, t => t.ToList());
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            var highlightingConsumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, File,
                DaemonProcess.ContextBoundSettingsStore);
            
            File.ProcessThisAndDescendants(this, highlightingConsumer);
            
            committer(new DaemonStageResult(highlightingConsumer.Highlightings));
        }

        public override void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            myContext.AdvanceContext(element, myProcessKind, myContextProviders);

            try
            {
                foreach (var (problemAnalyzerContext, problemAnalyzers) in myProblemAnalyzersByContext)
                {
                    if (!myContext.IsSuperSetOf(problemAnalyzerContext))
                        continue;

                    foreach (var problemAnalyzer in problemAnalyzers)
                        problemAnalyzer.RunInspection(element, DaemonProcess, myProcessKind, consumer, myContext);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                myLogger.Error(exception, "An exception occured during call graph problem analyzer execution");
            }
        }

        public override void ProcessAfterInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            base.ProcessAfterInterior(element, consumer);

            myContext.Rollback(element);
        }
    }
}