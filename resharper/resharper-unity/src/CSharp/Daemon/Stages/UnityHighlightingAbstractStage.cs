using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages
{
    public abstract class UnityHighlightingAbstractStage : CSharpDaemonStageBase
    {
        protected readonly IEnumerable<IUnityDeclarationHighlightingProvider> HighlightingProviders;
        protected readonly IEnumerable<IUnityProblemAnalyzer> ProblemAnalyzers;
        protected readonly UnityApi API;
        private readonly UnityCommonIconProvider myCommonIconProvider;
        protected readonly ILogger Logger;
        protected readonly UnityProblemAnalyzerContextSystem myContextSystem;

        protected UnityHighlightingAbstractStage(
            IEnumerable<IUnityDeclarationHighlightingProvider> highlightingProviders,
            IEnumerable<IUnityProblemAnalyzer> problemAnalyzers, UnityApi api,
            UnityCommonIconProvider commonIconProvider, ILogger logger, UnityProblemAnalyzerContextSystem contextSystem)
        {
            HighlightingProviders = highlightingProviders;
            ProblemAnalyzers = problemAnalyzers;
            API = api;
            myCommonIconProvider = commonIconProvider;
            Logger = logger;
            myContextSystem = contextSystem;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (!file.GetProject().IsUnityProject())
                return null;

            return new UnityHighlightingProcess(process, file,
                HighlightingProviders, ProblemAnalyzers,
                API, myCommonIconProvider, myContextSystem.GetManagerInstance(settings), processKind, Logger);
        }
    }

    public class UnityHighlightingProcess : CSharpDaemonStageProcessBase
    {
        private readonly IReadOnlyList<IUnityDeclarationHighlightingProvider> myDeclarationHighlightingProviders;
        private readonly IReadOnlyList<IUnityProblemAnalyzer> myProblemAnalyzers;
        private readonly UnityApi myAPI;
        private readonly UnityCommonIconProvider myCommonIconProvider;
        private readonly UnityProblemAnalyzerContextManagerInstance myManagerInstance;
        private readonly DaemonProcessKind myProcessKind;
        private readonly ILogger myLogger;
        private readonly ISet<IDeclaredElement> myMarkedDeclarations = new HashSet<IDeclaredElement>();
        private readonly JetHashSet<IMethod> myEventFunctions;

        private readonly Dictionary<UnityProblemAnalyzerContextElement, List<IUnityProblemAnalyzer>>
            myProblemAnalyzersByContext;

        private UnityProblemAnalyzerContext myContext;

        public UnityHighlightingProcess([NotNull] IDaemonProcess process, [NotNull] ICSharpFile file,
            IEnumerable<IUnityDeclarationHighlightingProvider> declarationHighlightingProviders,
            IEnumerable<IUnityProblemAnalyzer> problemAnalyzers, UnityApi api,
            UnityCommonIconProvider commonIconProvider, UnityProblemAnalyzerContextManagerInstance managerInstance,
            DaemonProcessKind processKind, ILogger logger)
            : base(process, file)
        {
            myDeclarationHighlightingProviders = declarationHighlightingProviders.ToList();
            myProblemAnalyzers = problemAnalyzers.ToList();
            myAPI = api;
            myContext = UnityProblemAnalyzerContext.EMPTY_INSTANCE;
            myCommonIconProvider = commonIconProvider;
            myManagerInstance = managerInstance;
            myProcessKind = processKind;
            myLogger = logger;

            myEventFunctions = DaemonProcess.CustomData.GetData(UnityEventFunctionAnalyzer.UnityEventFunctionNodeKey)
                ?.Where(t => t != null && t.IsValid()).ToJetHashSet();

            DaemonProcess.CustomData.PutData(UnityEventFunctionAnalyzer.UnityEventFunctionNodeKey, myEventFunctions);

            myProblemAnalyzersByContext = myProblemAnalyzers.GroupBy(t => t.Context)
                .ToDictionary(t => t.Key, t => t.ToList());
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            var highlightingConsumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, File,
                DaemonProcess.ContextBoundSettingsStore);

            File.ProcessThisAndDescendants(this, highlightingConsumer);

            foreach (var declaration in File.Descendants<ICSharpFunctionDeclaration>())
            {
                var declaredElement = declaration.DeclaredElement;

                if (declaredElement == null)
                    continue;

                if (myEventFunctions != null && Enumerable.Contains(myEventFunctions, declaredElement))
                {
                    var method = (declaredElement as IMethod).NotNull("method != null");
                    var eventFunction = myAPI.GetUnityEventFunction(method);

                    if (eventFunction == null) // happens after event function refactoring 
                        continue;

                    myCommonIconProvider.AddEventFunctionHighlighting(highlightingConsumer, method, eventFunction,
                        "Event function", myProcessKind);
                    myMarkedDeclarations.Add(method);
                }
                else
                {
                    if (myMarkedDeclarations.Contains(declaredElement))
                        continue;

                    myCommonIconProvider.AddFrequentlyCalledMethodHighlighting(highlightingConsumer, declaration,
                        "Frequently called", "Frequently called code", myProcessKind);
                }
            }

            committer(new DaemonStageResult(highlightingConsumer.Highlightings));
        }

        public override void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            myContext = myManagerInstance.CreateContext(myContext, element, myProcessKind);

            if (element is ICSharpDeclaration declaration)
            {
                foreach (var unityDeclarationHighlightingProvider in myDeclarationHighlightingProviders)
                {
                    var result =
                        unityDeclarationHighlightingProvider.AddDeclarationHighlighting(declaration, consumer,
                            myProcessKind);

                    if (result)
                        myMarkedDeclarations.Add(
                            declaration.DeclaredElement.NotNull("declaration.DeclaredElement != null"));
                }
            }

            try
            {
                foreach (var (problemAnalyzerContext, problemAnalyzers) in myProblemAnalyzersByContext)
                {
                    if (!myContext.IsSuperSetOf(problemAnalyzerContext))
                        continue;

                    foreach (var problemAnalyzer in problemAnalyzers)
                    {
                        if(myContext.ContainAny(problemAnalyzer.ProhibitedContext))
                            continue;
                        
                        problemAnalyzer.RunInspection(element, DaemonProcess, myProcessKind, consumer);
                    }
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

            myContext = myContext.Rollback(element);
        }
    }
}