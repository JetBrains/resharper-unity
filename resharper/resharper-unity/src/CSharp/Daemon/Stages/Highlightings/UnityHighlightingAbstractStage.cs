using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    public abstract class UnityHighlightingAbstractStage : CSharpDaemonStageBase
    {
        protected readonly IEnumerable<IUnityDeclarationHiglightingProvider> HiglightingProviders;
        protected readonly UnityApi API;
        protected readonly UnityHighlightingContributor UnityHighlightingContributor;

        public UnityHighlightingAbstractStage(IEnumerable<IUnityDeclarationHiglightingProvider> higlightingProviders, UnityApi api,
            UnityHighlightingContributor unityHighlightingContributor)
        {
            HiglightingProviders = higlightingProviders;
            API = api;
            UnityHighlightingContributor = unityHighlightingContributor;
        }
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (!file.GetProject().IsUnityProject())
                return null;
            
            return new UnityHighlightingProcess(process, file, HiglightingProviders, API, UnityHighlightingContributor, processKind);
        }
    }
    
    public class UnityHighlightingProcess : CSharpDaemonStageProcessBase
    {
        private readonly IEnumerable<IUnityDeclarationHiglightingProvider> myHiglightingProviders;
        private readonly UnityApi myAPI;
        private readonly UnityHighlightingContributor myUnityHighlightingContributor;
        private readonly DaemonProcessKind myProcessKind;
        private readonly ISet<IDeclaredElement> myMarkedDeclarations = new HashSet<IDeclaredElement>();
        private readonly JetHashSet<IMethod> myEventFunctions;

        public UnityHighlightingProcess([NotNull] IDaemonProcess process, [NotNull] ICSharpFile file,
            IEnumerable<IUnityDeclarationHiglightingProvider> higlightingProviders, UnityApi api,
            UnityHighlightingContributor unityHighlightingContributor, DaemonProcessKind processKind) : base(process, file)
        {
            myHiglightingProviders = higlightingProviders;
            myAPI = api;
            myUnityHighlightingContributor = unityHighlightingContributor;
            myProcessKind = processKind;

            myEventFunctions = DaemonProcess.CustomData.GetData(UnityEventFunctionAnalyzer.UnityEventFunctionNodeKey)
                ?.ToJetHashSet();
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
                
                if (myEventFunctions != null && myEventFunctions.Contains(declaredElement))
                {
                    var method = (declaredElement as IMethod).NotNull("method != null");
                    myUnityHighlightingContributor.AddUnityImplicitHighlightingForEventFunction(highlightingConsumer,  method,
                       myAPI.GetUnityEventFunction(method), myProcessKind);
                    myMarkedDeclarations.Add(method);
                }
                else
                {
                    if (myMarkedDeclarations.Contains(declaredElement))
                        continue;
                    
                    myUnityHighlightingContributor.AddHighlighting(highlightingConsumer, declaration,
                        "Frequently called code", "Frequently called", myProcessKind, true);
                }
            }
            
            committer(new DaemonStageResult(highlightingConsumer.Highlightings));
        }

        public override void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            if (element is ICSharpDeclaration declaration)
            {
                foreach (var unityDeclarationHiglightingProvider in myHiglightingProviders)
                {
                    var result = unityDeclarationHiglightingProvider.Analyze(declaration, consumer, myProcessKind);
                    if (result != null)
                        myMarkedDeclarations.Add(result);
                }
            }

        }
    }
}