using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
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
        private readonly PerformanceCriticalCodeCallGraphAnalyzer myPerformanceCriticalCodeCallGraphAnalyzer;
        private readonly SolutionAnalysisService mySwa;
        protected readonly IEnumerable<IUnityDeclarationHighlightingProvider> HiglightingProviders;
        protected readonly IEnumerable<IPerformanceProblemAnalyzer> PerformanceProblemAnalyzers;
        protected readonly UnityApi API;
        private readonly UnityCommonIconProvider myCommonIconProvider;
        protected readonly ILogger Logger;

        protected UnityHighlightingAbstractStage(CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceCriticalCodeCallGraphAnalyzer performanceCriticalCodeCallGraphAnalyzer,
            SolutionAnalysisService swa, IEnumerable<IUnityDeclarationHighlightingProvider> higlightingProviders,
            IEnumerable<IPerformanceProblemAnalyzer> performanceProblemAnalyzers, UnityApi api,
            UnityCommonIconProvider  commonIconProvider, ILogger logger)
        {
            myCallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            myPerformanceCriticalCodeCallGraphAnalyzer = performanceCriticalCodeCallGraphAnalyzer;
            mySwa = swa;
            HiglightingProviders = higlightingProviders;
            PerformanceProblemAnalyzers = performanceProblemAnalyzers;
            API = api;
            myCommonIconProvider = commonIconProvider;
            Logger = logger;
        }
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (!file.GetProject().IsUnityProject())
                return null;
            
            var enabled = settings.GetValue((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting);

            
            return new UnityHighlightingProcess(process, file, myCallGraphSwaExtensionProvider,
                myPerformanceCriticalCodeCallGraphAnalyzer, mySwa, enabled ,HiglightingProviders, PerformanceProblemAnalyzers,
                API, myCommonIconProvider, processKind, Logger);
        }
    }
    
    public class UnityHighlightingProcess : CSharpDaemonStageProcessBase
    {
        private readonly CallGraphSwaExtensionProvider myCallGraphSwaExtensionProvider;
        private readonly PerformanceCriticalCodeCallGraphAnalyzer myPerformanceCriticalCodeCallGraphAnalyzer;
        private readonly SolutionAnalysisService mySwa;
        private readonly bool myIsPerformanceAnalysisEnabled;
        private readonly IEnumerable<IUnityDeclarationHighlightingProvider> myDeclarationHighlightingProviders;
        private readonly IEnumerable<IPerformanceProblemAnalyzer> myPerformanceProblemAnalyzers;
        private readonly UnityApi myAPI;
        private readonly UnityCommonIconProvider myCommonIconProvider;
        private readonly DaemonProcessKind myProcessKind;
        private readonly ILogger myLogger;
        private readonly ISet<IDeclaredElement> myMarkedDeclarations = new HashSet<IDeclaredElement>();
        private readonly JetHashSet<IMethod> myEventFunctions;
        private readonly Stack<bool> myPerformanceCriticalContext = new Stack<bool>();

        public UnityHighlightingProcess([NotNull] IDaemonProcess process, [NotNull] ICSharpFile file,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceCriticalCodeCallGraphAnalyzer performanceCriticalCodeCallGraphAnalyzer,
            SolutionAnalysisService swa,
            bool isPerformanceAnalysisEnabled,
            IEnumerable<IUnityDeclarationHighlightingProvider> declarationHighlightingProviders,
            IEnumerable<IPerformanceProblemAnalyzer> performanceProblemAnalyzers, UnityApi api, UnityCommonIconProvider commonIconProvider,
            DaemonProcessKind processKind,
            ILogger logger) : base(process, file)
        {
            myCallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            myPerformanceCriticalCodeCallGraphAnalyzer = performanceCriticalCodeCallGraphAnalyzer;
            mySwa = swa;
            myIsPerformanceAnalysisEnabled = isPerformanceAnalysisEnabled;
            myDeclarationHighlightingProviders = declarationHighlightingProviders;
            myPerformanceProblemAnalyzers = performanceProblemAnalyzers;
            myAPI = api;
            myCommonIconProvider = commonIconProvider;
            myProcessKind = processKind;
            myLogger = logger;

            myEventFunctions = DaemonProcess.CustomData.GetData(UnityEventFunctionAnalyzer.UnityEventFunctionNodeKey)
                ?.Where(t => t != null && t.IsValid()).ToJetHashSet();
            
            DaemonProcess.CustomData.PutData(UnityEventFunctionAnalyzer.UnityEventFunctionNodeKey, myEventFunctions);
                
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            var highlightingConsumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, File,DaemonProcess.ContextBoundSettingsStore);
            File.ProcessThisAndDescendants(this, highlightingConsumer);

            foreach (var declaration in File.Descendants<ICSharpDeclaration>())
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
            if (IsMethodDeclaration(element))
                myPerformanceCriticalContext.Push(IsPerformanceCriticalDeclaration(element));

            if (element is ICSharpDeclaration declaration)
            {
                foreach (var unityDeclarationHiglightingProvider in myDeclarationHighlightingProviders)
                {
                    var result = unityDeclarationHiglightingProvider.AddDeclarationHighlighting(declaration, consumer, myProcessKind);
                    if (result)
                        myMarkedDeclarations.Add(declaration.DeclaredElement.NotNull("declaration.DeclaredElement != null"));
                }
            }

            if (myIsPerformanceAnalysisEnabled && myPerformanceCriticalContext.Count > 0 && myPerformanceCriticalContext.Peek())
            {
                try
                {
                    foreach (var performanceProblemAnalyzer in myPerformanceProblemAnalyzers)
                    {
                        performanceProblemAnalyzer.RunInspection(element, DaemonProcess, myProcessKind, consumer);
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
        }

        public override void ProcessAfterInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            base.ProcessAfterInterior(element, consumer);
            if (IsMethodDeclaration(element))
            {
                Assertion.Assert(myPerformanceCriticalContext.Count > 0, "myPerformanceCriticalContext.Count > 0");
                myPerformanceCriticalContext.Pop();
            }
        }


        private bool IsMethodDeclaration(ITreeNode node)
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
                var id = mySwa.GetElementId(declaredElement);
                if (!id.HasValue)
                    return false;

                return myCallGraphSwaExtensionProvider.IsMarkedByCallGraphAnalyzer(
                    myPerformanceCriticalCodeCallGraphAnalyzer.Id,
                    id.Value, true);
            }

            return PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(myAPI, declaration);
        }
    }
}