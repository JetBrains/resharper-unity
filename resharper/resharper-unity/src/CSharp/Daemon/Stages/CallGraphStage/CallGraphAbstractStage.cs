using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
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
        private readonly CallGraphSwaExtensionProvider mySwaExtensionProvider;
        private readonly IEnumerable<ICallGraphContextProvider> myContextProviders;
        private readonly IEnumerable<ICallGraphProblemAnalyzer> myProblemAnalyzers;
        private readonly ILogger myLogger;

        protected CallGraphAbstractStage(
            CallGraphSwaExtensionProvider swaExtensionProvider,
            IEnumerable<ICallGraphContextProvider> contextProviders,
            IEnumerable<ICallGraphProblemAnalyzer> problemAnalyzers,
            ILogger logger)
        {
            mySwaExtensionProvider = swaExtensionProvider;
            myContextProviders = contextProviders;
            myProblemAnalyzers = problemAnalyzers;
            myLogger = logger;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            var sourceFile = file.GetSourceFile();

            if (!file.GetProject().IsUnityProject() || !mySwaExtensionProvider.IsApplicable(sourceFile))
                return null;

            return new CallGraphProcess(process, processKind, file, myLogger, myContextProviders, myProblemAnalyzers);
        }
    }

    public class CallGraphProcess : CSharpDaemonStageProcessBase
    {
        private readonly ILogger myLogger;
        private readonly IEnumerable<ICallGraphContextProvider> myContextProviders;
        private readonly IEnumerable<ICallGraphProblemAnalyzer> myProblemAnalyzers;
        private readonly CallGraphContext myContext;
        private int myInterruptCounter;

        public CallGraphProcess(
            IDaemonProcess process,
            DaemonProcessKind processKind,
            ICSharpFile file,
            ILogger logger,
            IEnumerable<ICallGraphContextProvider> contextProviders,
            IEnumerable<ICallGraphProblemAnalyzer> problemAnalyzers)
            : base(process, file)
        {
            myContext = new CallGraphContext(processKind, process); 
            myLogger = logger;
            myContextProviders = contextProviders;
            myProblemAnalyzers = problemAnalyzers;
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            if (myContext.Kind != DaemonProcessKind.GLOBAL_WARNINGS && myContext.Kind != DaemonProcessKind.VISIBLE_DOCUMENT) 
                return;

            File.GetPsiServices().Locks.AssertReadAccessAllowed();
            
            var highlightingConsumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, File,
                DaemonProcess.ContextBoundSettingsStore);

            File.ProcessThisAndDescendants(this, highlightingConsumer);

            committer(new DaemonStageResult(highlightingConsumer.Highlightings));
        }

        private void CheckForInterrupt(IHighlightingConsumer consumer)
        {
            var counter = ++myInterruptCounter;
            
            if (counter == 10)
            {
                myInterruptCounter = 0;
                IsProcessingFinished(consumer);
            }
        }

        public override void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            myContext.AdvanceContext(element, myContextProviders);

            try
            {
                foreach (var problemAnalyzer in myProblemAnalyzers)
                {
                    CheckForInterrupt(consumer);
                    problemAnalyzer.RunInspection(element, consumer, myContext);
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
            myContext.Rollback(element);
        }
    }
}