using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Help;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Help;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    [SolutionComponent]
    public class UnityHighlightingContributor
    {
        protected SolutionAnalysisService Swa { get; }
        protected int PerformanceAnalyzerId { get; }
        protected readonly ISolution Solution;
        protected readonly ITextControlManager TextControlManager;
        protected readonly IContextBoundSettingsStore SettingsStore;

        public UnityHighlightingContributor(ISolution solution, ISettingsStore settingsStore,
            ITextControlManager textControlManager, SolutionAnalysisService swa,
            PerformanceCriticalCodeCallGraphAnalyzer performanceCriticalCodeCallGraphAnalyzer)
        {
            Swa = swa;
            PerformanceAnalyzerId = performanceCriticalCodeCallGraphAnalyzer.AnalyzerId;
            Solution = solution;
            TextControlManager = textControlManager;
            SettingsStore = settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
        }

        public virtual void AddUnityImplicitHighlightingForEventFunction(IHighlightingConsumer consumer, IMethod method,
            UnityEventFunction eventFunction, DaemonProcessKind kind)
        {
            var message = GetMessageForUnityEventFunction(eventFunction);
            foreach (var declaration in method.GetDeclarations())
            {
                if (declaration is ICSharpDeclaration cSharpDeclaration)
                    AddHighlightingWithConfigurableHighlighter(consumer, cSharpDeclaration, message, "Event function",
                        kind);
            }
        }

        public virtual string GetMessageForUnityEventFunction(UnityEventFunction eventFunction)
        {
            var tooltip = "Unity event function";
            if (!string.IsNullOrEmpty(eventFunction.Description))
                tooltip += Environment.NewLine + Environment.NewLine + eventFunction.Description;
            if (eventFunction.Coroutine)
                tooltip += Environment.NewLine + "This function can be a coroutine.";

            return tooltip;
        }
        
        public virtual void AddUnityImplicitClassUsage(IHighlightingConsumer consumer,
            IClassLikeDeclaration declaration, string tooltip, string displayName, DaemonProcessKind kind)
        {
            AddHighlightingWithConfigurableHighlighter(consumer, declaration, tooltip, displayName, kind);
        }

        public virtual void AddInitializeOnLoadMethod(IHighlightingConsumer consumer,
            IConstructorDeclaration constructorDeclaration, string tooltip, string displayName, DaemonProcessKind kind)
        {
            AddHighlightingWithConfigurableHighlighter(consumer, constructorDeclaration, tooltip, displayName, kind);
        }

        public virtual void AddUnityImplicitFieldUsage(IHighlightingConsumer consumer, IFieldDeclaration field,
            string tooltip, string displayName, DaemonProcessKind kind)
        {
            AddHighlightingWithConfigurableHighlighter(consumer, field, tooltip, displayName, kind);
        }

        public virtual void AddUnityEventHandler(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip,
            string displayName, DaemonProcessKind kind)
        {
            AddHighlightingWithConfigurableHighlighter(consumer, element, tooltip, displayName, kind);
        }

        public virtual void AddHighlightingWithConfigurableHighlighter(IHighlightingConsumer consumer,
            ICSharpDeclaration element, string tooltip, string displayName, DaemonProcessKind kind)
        {
            AddHighlighting(consumer, element, tooltip, displayName, kind);
            AddConfigurableHighlighter(consumer, element, kind);
        }

        public virtual void AddConfigurableHighlighter(IHighlightingConsumer consumer, ICSharpDeclaration element,
            DaemonProcessKind kind)
        {
            consumer.AddHighlighting(
                new UnityImplicitlyUsedIdentifierHighlighting(element.NameIdentifier.GetDocumentRange()));
        }

        public virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip,
            string displayName, DaemonProcessKind kind, bool addOnlyHotIcon = false)
        {
            var mode = SettingsStore.GetValue((UnitySettings key) => key.GutterIconMode);
            if (mode == GutterIconMode.None)
                return;

            var isIconHot = IsHotIcon(element, kind);
            if (addOnlyHotIcon && !isIconHot)
                return;
            
            var highlighting = isIconHot
                ? new UnityHotGutterMarkInfo(element, tooltip)
                : (IHighlighting) new UnityGutterMarkInfo(element, tooltip);
            consumer.AddHighlighting(highlighting);
        }
        
        protected virtual bool IsHotIcon(ICSharpDeclaration element, DaemonProcessKind kind)
        {
            if (!SettingsStore.GetValue((UnitySettings key) => key.EnableIconsForPerformanceCriticalCode))
                return false;
            
            if (!SettingsStore.GetValue((UnitySettings key) => key.EnablePerformanceCriticalCodeHighlighting))
                return false;
            
            var declaredElement = element.DeclaredElement;
            if (declaredElement == null)
                return false;
            
            var usageChecker = Swa.UsageChecker;
            if (usageChecker == null)
                return false;

            var id = Swa.GetElementId(declaredElement, true);
            if (!id.HasValue)
                return false;

            return usageChecker.IsMarkedByCallGraphAnalyzer(PerformanceAnalyzerId, id.Value, kind == DaemonProcessKind.GLOBAL_WARNINGS);
        }

        public IEnumerable<BulbMenuItem> CreateBulbItemsForUnityDeclaration(IDeclaration declaration)
        {
            if (!declaration.IsValid())
                return EmptyList<BulbMenuItem>.Enumerable;
            
            var unityApi = Solution.GetComponent<UnityApi>();
            var assetSerializationMode = Solution.GetComponent<AssetSerializationMode>();
            var textControl = TextControlManager.LastFocusedTextControl.Value;
            if (textControl != null)
            {
                var result = new List<BulbMenuItem>();
                if (declaration is IClassLikeDeclaration classDeclaration && unityApi.IsUnityType(declaration.DeclaredElement as ITypeElement))
                {
                    var fix = new GenerateUnityEventFunctionsFix(classDeclaration);
                    result.Add(
                        new BulbMenuItem(new IntentionAction.MyExecutableProxi(fix, Solution, textControl),
                            "Generate Unity event functions", PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id,
                            BulbMenuAnchors.FirstClassContextItems)
                    );
                }

                if (declaration is IMethodDeclaration methodDeclaration)
                {
                    var declaredElement = methodDeclaration.DeclaredElement;

                    if (declaredElement != null)
                    {
                        var isCoroutine = IsCoroutine(methodDeclaration, unityApi);
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

                        if (unityApi.IsEventFunction(declaredElement))
                        {
                            var documentationNavigationAction = new DocumentationNavigationAction(
                                Solution.GetComponent<ShowUnityHelp>(), declaredElement, unityApi);
                            result.Add(new BulbMenuItem(
                                new IntentionAction.MyExecutableProxi(documentationNavigationAction, Solution,
                                    textControl), documentationNavigationAction.Text, BulbThemedIcons.ContextAction.Id,
                                BulbMenuAnchors.FirstClassContextItems));
                        }
                    }
                }

                result.AddRange(CreateAdditionalMenuItem(declaration, unityApi, assetSerializationMode, textControl));
                return result;
            }

            return EmptyList<BulbMenuItem>.Enumerable;
        }

        // TODO, create provider for bulb actions 
        public virtual IEnumerable<BulbMenuItem> CreateAdditionalMenuItem(IDeclaration declaration, UnityApi api, 
            AssetSerializationMode assetSerializationMode, ITextControl textControl)
        {
            return EmptyList<BulbMenuItem>.Enumerable;
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
            if (type == null) return null;

            return Equals(type.GetClrName(), PredefinedType.IENUMERATOR_FQN);
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
    }
}