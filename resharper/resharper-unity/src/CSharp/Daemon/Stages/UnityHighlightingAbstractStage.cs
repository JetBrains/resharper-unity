using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages
{
    public abstract class UnityHighlightingAbstractStage : CSharpDaemonStageBase
    {
        private readonly CallGraphSwaExtensionProvider myCallGraphSwaExtensionProvider;
        private readonly PerformanceCriticalCodeCallGraphMarksProvider myPerformanceCriticalCodeCallGraphMarksProvider;
        private readonly CallGraphBurstMarksProvider myCallGraphBurstMarksProvider;
        protected readonly IEnumerable<IUnityDeclarationHighlightingProvider> HiglightingProviders;
        protected readonly IEnumerable<IUnityProblemAnalyzer> PerformanceProblemAnalyzers;
        protected readonly UnityApi API;
        private readonly UnityCommonIconProvider myCommonIconProvider;
        private readonly IElementIdProvider myProvider;
        protected readonly ILogger Logger;

        protected UnityHighlightingAbstractStage(CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceCriticalCodeCallGraphMarksProvider performanceCriticalCodeCallGraphMarksProvider,
            CallGraphBurstMarksProvider callGraphBurstMarksProvider,
            IEnumerable<IUnityDeclarationHighlightingProvider> higlightingProviders,
            IEnumerable<IUnityProblemAnalyzer> performanceProblemAnalyzers, UnityApi api,
            UnityCommonIconProvider commonIconProvider, IElementIdProvider provider, ILogger logger)
        {
            myCallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            myPerformanceCriticalCodeCallGraphMarksProvider = performanceCriticalCodeCallGraphMarksProvider;
            myCallGraphBurstMarksProvider = callGraphBurstMarksProvider;
            HiglightingProviders = higlightingProviders;
            PerformanceProblemAnalyzers = performanceProblemAnalyzers;
            API = api;
            myCommonIconProvider = commonIconProvider;
            myProvider = provider;
            Logger = logger;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (!file.GetProject().IsUnityProject())
                return null;

            var isPerformanceAnalysisEnabled =
                settings.GetValue((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);
            var isBurstAnalysisEnabled = settings.GetValue((UnitySettings s) => s.EnableBurstCodeHighlighting);


            return new UnityHighlightingProcess(process, file, myCallGraphSwaExtensionProvider,
                myPerformanceCriticalCodeCallGraphMarksProvider, isPerformanceAnalysisEnabled,
                myCallGraphBurstMarksProvider, isBurstAnalysisEnabled,
                HiglightingProviders, PerformanceProblemAnalyzers,
                API, myCommonIconProvider, processKind, myProvider, Logger);
        }
    }

    public class UnityHighlightingProcess : CSharpDaemonStageProcessBase
    {
        private readonly CallGraphSwaExtensionProvider myCallGraphSwaExtensionProvider;
        private readonly PerformanceCriticalCodeCallGraphMarksProvider myPerformanceCriticalCodeCallGraphMarksProvider;
        private readonly bool myIsPerformanceAnalysisEnabled;
        private readonly CallGraphBurstMarksProvider myCallGraphBurstMarksProvider;
        private readonly bool myIsBurstAnalysisEnabled;
        private readonly IEnumerable<IUnityDeclarationHighlightingProvider> myDeclarationHighlightingProviders;
        private readonly IEnumerable<IUnityProblemAnalyzer> myPerformanceProblemAnalyzers;
        private readonly UnityApi myAPI;
        private readonly UnityCommonIconProvider myCommonIconProvider;
        private readonly DaemonProcessKind myProcessKind;
        private readonly IElementIdProvider myProvider;
        private readonly ILogger myLogger;
        private readonly ISet<IDeclaredElement> myMarkedDeclarations = new HashSet<IDeclaredElement>();
        private readonly JetHashSet<IMethod> myEventFunctions;

        private readonly Dictionary<UnityProblemAnalyzerContext, List<IUnityProblemAnalyzer>>
            myProblemAnalyzersByContext;

        private readonly Stack<List<UnityProblemAnalyzerContext>> myProblemAnalyzerContexts =
            new Stack<List<UnityProblemAnalyzerContext>>();

        private readonly Stack<UnityProblemAnalyzerContext> myProhibitedContexts =
            new Stack<UnityProblemAnalyzerContext>();

        public UnityHighlightingProcess([NotNull] IDaemonProcess process, [NotNull] ICSharpFile file,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceCriticalCodeCallGraphMarksProvider performanceCriticalCodeCallGraphMarksProvider,
            bool isPerformanceAnalysisEnabled,
            CallGraphBurstMarksProvider callGraphBurstMarksProvider, bool isBurstAnalysisEnabled,
            IEnumerable<IUnityDeclarationHighlightingProvider> declarationHighlightingProviders,
            IEnumerable<IUnityProblemAnalyzer> performanceProblemAnalyzers, UnityApi api,
            UnityCommonIconProvider commonIconProvider,
            DaemonProcessKind processKind, IElementIdProvider provider,
            ILogger logger)
            : base(process, file)
        {
            myCallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            myPerformanceCriticalCodeCallGraphMarksProvider = performanceCriticalCodeCallGraphMarksProvider;
            myIsPerformanceAnalysisEnabled = isPerformanceAnalysisEnabled;
            myCallGraphBurstMarksProvider = callGraphBurstMarksProvider;
            myIsBurstAnalysisEnabled = isBurstAnalysisEnabled;
            myDeclarationHighlightingProviders = declarationHighlightingProviders;
            myPerformanceProblemAnalyzers = performanceProblemAnalyzers;
            myAPI = api;
            myCommonIconProvider = commonIconProvider;
            myProcessKind = processKind;
            myProvider = provider;
            myLogger = logger;

            myEventFunctions = DaemonProcess.CustomData.GetData(UnityEventFunctionAnalyzer.UnityEventFunctionNodeKey)
                ?.Where(t => t != null && t.IsValid()).ToJetHashSet();

            DaemonProcess.CustomData.PutData(UnityEventFunctionAnalyzer.UnityEventFunctionNodeKey, myEventFunctions);

            myProblemAnalyzersByContext = myPerformanceProblemAnalyzers.GroupBy(t => t.Context)
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

        private List<UnityProblemAnalyzerContext> GetProblemAnalyzerContext(ITreeNode element)
        {
            var res = new List<UnityProblemAnalyzerContext>();
            if (myIsPerformanceAnalysisEnabled && IsPerformanceCriticalDeclaration(element))
                res.Add(UnityProblemAnalyzerContext.PERFOMANCE_CONTEXT);
            if (myIsBurstAnalysisEnabled && IsBurstDeclaration(element))
                res.Add(UnityProblemAnalyzerContext.BURST_CONTEXT);
            return res;
        }

        private bool IsContextProhibited(UnityProblemAnalyzerContext context)
        {
            return myProhibitedContexts.Count > 0 && myProhibitedContexts.Peek().HasFlag(context);
        }

        private bool IsProhibitedNode(ITreeNode node)
        {
            switch (node)
            {
                case IThrowStatement _:
                case IThrowExpression _:
                    return true;
                default:
                    return false;
            }
        }

        private UnityProblemAnalyzerContext GetProhibitedContexts(ITreeNode node)
        {
            var context = UnityProblemAnalyzerContext.NONE;
            if (node is IThrowStatement || node is IThrowExpression)
                context |= UnityProblemAnalyzerContext.BURST_CONTEXT;
            return context;
        }

        public override void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            if (IsFunctionNode(element))
                myProblemAnalyzerContexts.Push(GetProblemAnalyzerContext(element));
            if (IsProhibitedNode(element))
                myProhibitedContexts.Push(GetProhibitedContexts(element));

            if (element is ICSharpDeclaration declaration)
            {
                foreach (var unityDeclarationHiglightingProvider in myDeclarationHighlightingProviders)
                {
                    var result =
                        unityDeclarationHiglightingProvider.AddDeclarationHighlighting(declaration, consumer,
                            myProcessKind);
                    if (result)
                        myMarkedDeclarations.Add(
                            declaration.DeclaredElement.NotNull("declaration.DeclaredElement != null"));
                }
            }

            try
            {
                if (myProblemAnalyzerContexts.Count > 0)
                {
                    foreach (var context in myProblemAnalyzerContexts.Peek())
                        if (!IsContextProhibited(context))
                        {
                            foreach (var performanceProblemAnalyzer in myProblemAnalyzersByContext[context])
                            {
                                performanceProblemAnalyzer.RunInspection(element, DaemonProcess, myProcessKind,
                                    consumer);
                            }
                        }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                myLogger.Error(exception, "An exception occured during performance problem analyzer execution");
            }
        }

        public override void ProcessAfterInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            base.ProcessAfterInterior(element, consumer);
            if (IsFunctionNode(element))
            {
                Assertion.Assert(myProblemAnalyzerContexts.Count > 0, "myProblemAnalyzerContexts.Count > 0");
                myProblemAnalyzerContexts.Pop();
            }

            if (IsProhibitedNode(element))
            {
                Assertion.Assert(myProhibitedContexts.Count > 0, "myProhibitedContexts.Count > 0");
                myProhibitedContexts.Pop();
            }
        }


        private bool IsFunctionNode(ITreeNode node)
        {
            switch (node)
            {
                case IFunctionDeclaration _:
                case ICSharpClosure _:
                    return true;
                default:
                    return false;
            }
        }


        private bool IsPerformanceCriticalDeclaration(ITreeNode element)
        {
            if (!(element is ICSharpDeclaration declaration))
                return false;

            var declaredElement = declaration.DeclaredElement;
            if (declaredElement == null)
                return false;

            if (myProcessKind == DaemonProcessKind.GLOBAL_WARNINGS)
            {
                var id = myProvider.GetElementId(declaredElement);
                if (!id.HasValue)
                    return false;

                return myCallGraphSwaExtensionProvider.IsMarkedByCallGraphAnalyzer(
                    myPerformanceCriticalCodeCallGraphMarksProvider.Id,
                    true, id.Value);
            }

            return PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(myAPI, declaration);
        }

        private bool IsBurstDeclaration(ITreeNode element)
        {
            if (!(element is ICSharpDeclaration declaration))
                return false;
            var declaredElement = declaration.DeclaredElement;
            if (declaredElement == null)
                return false;
            if (myProcessKind == DaemonProcessKind.GLOBAL_WARNINGS)
            {
                var id = myProvider.GetElementId(declaredElement);
                if (!id.HasValue)
                    return false;
                return myCallGraphSwaExtensionProvider.IsMarkedByCallGraphAnalyzer(myCallGraphBurstMarksProvider.Id,
                    true,
                    id.Value);
            }

            return myCallGraphBurstMarksProvider.GetMarkedFunctionsFrom(element, null).FirstOrDefault() != null;
        }
    }
}