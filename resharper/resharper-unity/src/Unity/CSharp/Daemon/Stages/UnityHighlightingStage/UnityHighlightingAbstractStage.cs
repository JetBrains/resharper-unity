using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.UnityHighlightingStage
{
    public abstract class UnityHighlightingAbstractStage : CSharpDaemonStageBase
    {
        protected readonly IImmutableEnumerable<IUnityDeclarationHighlightingProvider> HighlightingProviders;
        protected readonly UnityApi API;
        protected readonly UnityCommonIconProvider CommonIconProvider;

        protected UnityHighlightingAbstractStage(
            IImmutableEnumerable<IUnityDeclarationHighlightingProvider> highlightingProviders,
            UnityApi api,
            UnityCommonIconProvider commonIconProvider)
        {
            HighlightingProviders = highlightingProviders;
            API = api;
            CommonIconProvider = commonIconProvider;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (!file.GetProject().IsUnityProject())
                return null;

            return new UnityHighlightingProcess(process, file, HighlightingProviders, API,
                CommonIconProvider, processKind);
        }
    }

    public class UnityHighlightingProcess : CSharpDaemonStageProcessBase
    {
        private readonly IImmutableEnumerable<IUnityDeclarationHighlightingProvider> myDeclarationHighlightingProviders;
        private readonly UnityApi myAPI;
        private readonly UnityCommonIconProvider myCommonIconProvider;
        private readonly ISet<IDeclaredElement> myMarkedDeclarations = new HashSet<IDeclaredElement>();
        private readonly JetHashSet<IMethod> myEventFunctions;
        private readonly CallGraphContext myContext;

        public UnityHighlightingProcess(
            [NotNull] IDaemonProcess process, 
            [NotNull] ICSharpFile file,
            IImmutableEnumerable<IUnityDeclarationHighlightingProvider> declarationHighlightingProviders,
            UnityApi api,
            UnityCommonIconProvider commonIconProvider,
            DaemonProcessKind processKind)
            : base(process, file)
        {
            myContext = new CallGraphContext(processKind, process);
            myDeclarationHighlightingProviders = declarationHighlightingProviders;
            myAPI = api;
            myCommonIconProvider = commonIconProvider;

            myEventFunctions = DaemonProcess.CustomData.GetData(UnityEventFunctionAnalyzer.UnityEventFunctionNodeKey)
                ?.Where(t => t != null && t.IsValid()).ToJetHashSet();

            DaemonProcess.CustomData.PutData(UnityEventFunctionAnalyzer.UnityEventFunctionNodeKey, myEventFunctions);
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
                        Strings.UnityHighlightingProcess_Event_function, myContext);
                    myMarkedDeclarations.Add(method);
                }
                else
                {
                    if (myMarkedDeclarations.Contains(declaredElement))
                        continue;

                    myCommonIconProvider.AddFrequentlyCalledMethodHighlighting(highlightingConsumer, declaration,
                        Strings.UnityHighlightingProcess_Execute_Frequently_called, Strings.UnityHighlightingProcess_Execute_Frequently_called_code, myContext);
                }
            }

            committer(new DaemonStageResult(highlightingConsumer.CollectHighlightings()));
        }

        public override void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            if (!(element is ICSharpDeclaration declaration)) 
                return;
            
            foreach (var unityDeclarationHighlightingProvider in myDeclarationHighlightingProviders)
            {
                var result = unityDeclarationHighlightingProvider.AddDeclarationHighlighting(declaration, consumer, myContext);

                if (result)
                    myMarkedDeclarations.Add(declaration.DeclaredElement.NotNull("declaration.DeclaredElement != null"));
            }
        }
    }
}