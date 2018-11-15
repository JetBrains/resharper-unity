using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Help;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Help;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    [SolutionComponent]
    public class UnityImplicitUsageHighlightingContributor
    {
        protected readonly ISolution Solution;
        protected readonly ITextControlManager TextControlManager;
        protected readonly IContextBoundSettingsStore SettingsStore;

        public UnityImplicitUsageHighlightingContributor(ISolution solution, ISettingsStore settingsStore,
            ITextControlManager textControlManager)
        {
            Solution = solution;
            TextControlManager = textControlManager;
            SettingsStore = settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
        }


        public virtual void AddUnityImplicitHighlightingForEventFunction(IHighlightingConsumer consumer, IMethod method, UnityEventFunction eventFunction)
        {
            var tooltip = "Unity event function";
            if (!string.IsNullOrEmpty(eventFunction.Description))
                tooltip += Environment.NewLine + Environment.NewLine + eventFunction.Description;
            if (eventFunction.Coroutine)
                tooltip += Environment.NewLine + "This function can be a coroutine.";

            foreach (var declaration in method.GetDeclarations())
            {
                if (declaration is ICSharpDeclaration cSharpDeclaration)
                    AddHighlightingWithConfigurableHighlighter(consumer, cSharpDeclaration, tooltip);
            }
        }

        public virtual void AddUnityImplicitClassUsage(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string tooltip)
        {
            AddHighlightingWithConfigurableHighlighter(consumer, declaration, tooltip);
        }
        
        public virtual void AddInitializeOnLoadMethod(IHighlightingConsumer consumer, IConstructorDeclaration constructorDeclaration, string tooltip)
        {
            AddHighlightingWithConfigurableHighlighter(consumer, constructorDeclaration, tooltip);
        }

        public virtual void AddUnityImplicitFieldUsage(IHighlightingConsumer consumer, IFieldDeclaration field, string tooltip)
        {
            AddHighlightingWithConfigurableHighlighter(consumer, field, tooltip);
        }

        public virtual void AddUnityEventHandler(IHighlightingConsumer consumer, IDeclaration element, string tooltip)
        {
            if (element is ICSharpDeclaration cSharpDeclaration)
                AddHighlightingWithConfigurableHighlighter(consumer, cSharpDeclaration, tooltip);
        }

        public virtual void AddHighlightingWithConfigurableHighlighter(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip)
        {
            AddHighlighting(consumer, element, tooltip);
            AddConfigurableHighlighter(consumer, element);
        }

        public virtual void AddConfigurableHighlighter(IHighlightingConsumer consumer, ICSharpDeclaration element)
        {
            if (SettingsStore.GetValue((UnitySettings key) => key.GutterIconMode) == GutterIconMode.None)
                return;
            
            consumer.AddHighlighting(new UnityImplicitlyUsedIdentifierHighlighting(element.NameIdentifier.GetDocumentRange()));
        }
        
        public virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip)
        {
            var mode = SettingsStore.GetValue((UnitySettings key) => key.GutterIconMode);
            if (mode == GutterIconMode.None)
                return;
            
            var highlighting = new UnityGutterMarkInfo(element, tooltip);
            consumer.AddHighlighting(highlighting);
        }
        
        public IEnumerable<BulbMenuItem> CreateBulbItemsForUnityDeclaration(IDeclaration declaration)
        {
            var unityApi = Solution.GetComponent<UnityApi>();
            var textControl = TextControlManager.LastFocusedTextControl.Value;
            if (textControl != null)
            {

                if (declaration is IClassLikeDeclaration classDeclaration)
                {
                    var fix = new GenerateUnityEventFunctionsFix(classDeclaration);
                    return new[]
                    {
                        new BulbMenuItem(new IntentionAction.MyExecutableProxi(fix, Solution, textControl),
                            "Generate Unity event functions", PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id,
                            BulbMenuAnchors.FirstClassContextItems)
                    };
                }

                if (declaration is IMethodDeclaration methodDeclaration)
                {
                    var result = new List<BulbMenuItem>();
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
                            var navigation = new NavigationAction(Solution.GetComponent<ShowUnityHelp>(), declaredElement,
                                unityApi);
                            result.Add(new BulbMenuItem(
                                new IntentionAction.MyExecutableProxi(navigation, Solution, textControl),
                                navigation.Text, BulbThemedIcons.ContextAction.Id,
                                BulbMenuAnchors.FirstClassContextItems));
                        }

                        return result;
                    }
                }
            }

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

        internal class NavigationAction : BulbActionBase
        {
            private readonly ShowUnityHelp myShowUnityHelp;
            private readonly IMethod myMethod;
            private readonly UnityApi myUnityApi;

            public NavigationAction(ShowUnityHelp showUnityHelp, IMethod method, UnityApi unityApi)
            {
                myShowUnityHelp = showUnityHelp;
                myMethod = method;
                myUnityApi = unityApi;
            }
            
            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                myShowUnityHelp.ShowHelp(myMethod.GetUnityEventFunctionName(myUnityApi), HelpSystem.HelpKind.Msdn);
                return null;
            }

            public override string Text => "Open documentation";
        }
    }
}