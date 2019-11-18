using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Help;
using JetBrains.Application.UI.Icons.CommonThemedIcons;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Application.UI.Help;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class UnityCommonIconProvider
    {
        protected readonly ISolution Solution;
        protected readonly SolutionAnalysisService Swa;
        protected readonly CallGraphSwaExtensionProvider CallGraphSwaExtensionProvider;
        protected readonly PerformanceCriticalCodeCallGraphAnalyzer Analyzer;
        protected readonly UnityApi UnityApi;
        protected readonly IContextBoundSettingsStore Settings;

        public UnityCommonIconProvider(ISolution solution, SolutionAnalysisService swa, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            SettingsStore settingsStore, PerformanceCriticalCodeCallGraphAnalyzer analyzer, UnityApi unityApi)
        {
            Solution = solution;
            Swa = swa;
            CallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            Analyzer = analyzer;
            UnityApi = unityApi;
            Settings = settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
        }

        public virtual void AddEventFunctionHighlighting(IHighlightingConsumer consumer, IMethod method,
            UnityEventFunction eventFunction, string text, DaemonProcessKind kind)
        {
            foreach (var declaration in method.GetDeclarations())
            {
                if (declaration is ICSharpDeclaration cSharpDeclaration)
                {
                    consumer.AddImplicitConfigurableHighlighting(cSharpDeclaration);
                    consumer.AddHotHighlighting(Swa, CallGraphSwaExtensionProvider, cSharpDeclaration, Analyzer, Settings, text,
                        GetEventFunctionTooltip(eventFunction), kind, GetEventFunctionActions(cSharpDeclaration));

                }
            }
        }

        public virtual void AddFrequentlyCalledMethodHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration declaration,
            string text, string tooltip, DaemonProcessKind kind)
        {
            consumer.AddHotHighlighting(Swa, CallGraphSwaExtensionProvider, declaration, Analyzer, Settings, text, tooltip, kind, EnumerableCollection<BulbMenuItem>.Empty, true);
        }

        protected IEnumerable<BulbMenuItem> GetEventFunctionActions(ICSharpDeclaration declaration)
        {
            var result = new List<BulbMenuItem>();
            if (declaration is IMethodDeclaration methodDeclaration)
            {
                var declaredElement = methodDeclaration.DeclaredElement;
                var textControl = Solution.GetComponent<ITextControlManager>().LastFocusedTextControl.Value;

                if (textControl != null && declaredElement != null)
                {
                    var isCoroutine = IsCoroutine(methodDeclaration, UnityApi);
                    if (isCoroutine.HasValue)
                    {
                        IBulbAction bulbAction;
                        if (isCoroutine.Value)
                            bulbAction = new ConvertFromCoroutineBulbAction(methodDeclaration);
                        else
                            bulbAction = new ConvertToCoroutineBulbAction(methodDeclaration);

                        result.Add(new BulbMenuItem(
                            new IntentionAction.MyExecutableProxi(bulbAction, Solution, textControl),
                            bulbAction.Text, BulbThemedIcons.ContextAction.Id,
                            BulbMenuAnchors.FirstClassContextItems));
                    }

                    if (UnityApi.IsEventFunction(declaredElement))
                    {
                        var documentationNavigationAction = new DocumentationNavigationAction(
                            Solution.GetComponent<ShowUnityHelp>(), declaredElement, UnityApi);
                        result.Add(new BulbMenuItem(
                            new IntentionAction.MyExecutableProxi(documentationNavigationAction, Solution,
                                textControl), documentationNavigationAction.Text, CommonThemedIcons.Question.Id,
                            BulbMenuAnchors.FirstClassContextItems));
                    }
                }
            }

            return result;
        }

        protected virtual string GetEventFunctionTooltip(UnityEventFunction eventFunction)
        {
            var tooltip = "Unity event function";
            if (!string.IsNullOrEmpty(eventFunction.Description))
                tooltip += Environment.NewLine + Environment.NewLine + eventFunction.Description;
            if (eventFunction.Coroutine)
                tooltip += Environment.NewLine + "This function can be a coroutine.";

            return tooltip;
        }

        private class DocumentationNavigationAction : BulbActionBase
        {
            private readonly ShowUnityHelp myShowUnityHelp;
            private readonly IMethod myMethod;
            private readonly UnityApi myUnityApi;

            public DocumentationNavigationAction(ShowUnityHelp showUnityHelp, IMethod method, UnityApi unityApi)
            {
                myShowUnityHelp = showUnityHelp;
                myMethod = method;
                myUnityApi = unityApi;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution,
                IProgressIndicator progress)
            {
                myShowUnityHelp.ShowHelp(myMethod.GetUnityEventFunctionName(myUnityApi), HelpSystem.HelpKind.Msdn);
                return null;
            }

            public override string Text => "View documentation";
        }

        protected static bool? IsCoroutine(IMethodDeclaration methodDeclaration, UnityApi unityApi)
        {
            if (methodDeclaration == null) return null;
            if (!methodDeclaration.IsFromUnityProject()) return null;

            var method = methodDeclaration.DeclaredElement;
            if (method == null) return null;

            var function = unityApi.GetUnityEventFunction(method);
            if (function == null || !function.Coroutine) return null;

            var type = method.ReturnType.GetScalarType();
            if (type == null) return false;

            return Equals(type.GetClrName(), PredefinedType.IENUMERATOR_FQN);
        }
    }
}