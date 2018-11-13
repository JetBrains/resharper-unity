using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.TextControl;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.Util;

[assembly: RegisterHighlighter(UnityHighlightingAttributeIds.UNITY_IMPLICIT_USAGE_BOLD_ATTRIBUTE,
    EffectType = EffectType.TEXT, FontStyle = FontStyle.Bold, Layer = HighlighterLayer.SYNTAX + 1)]

namespace JetBrains.ReSharper.Plugins.Unity
{
    [StaticSeverityHighlighting(Severity.INFO, "UnityGutterMarks", Languages = "CSHARP", OverlapResolve = OverlapResolveKind.NONE)]
    public class UnityImplicitBoldHighlighting : ICustomAttributeIdHighlighting
    {
        private readonly DocumentRange myDocumentRange;

        public UnityImplicitBoldHighlighting(string toolTip, DocumentRange documentRange)
        {
            myDocumentRange = documentRange;
            ToolTip = toolTip;
        }

        public bool IsValid() => true;

        public DocumentRange CalculateRange() => myDocumentRange;

        public string ToolTip { get; }
        public string ErrorStripeToolTip => null;
        public string AttributeId => UnityHighlightingAttributeIds.UNITY_IMPLICIT_USAGE_BOLD_ATTRIBUTE;
    }
    
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
                    AddHighlightingWithBold(consumer, cSharpDeclaration, tooltip);
            }
        }

        public virtual void AddUnityImplicitClassUsage(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string tooltip)
        {
            AddHighlightingWithBold(consumer, declaration, tooltip);
        }
        
        public virtual void AddUnityStartMethod(IHighlightingConsumer consumer, IConstructorDeclaration constructorDeclaration, string tooltip)
        {
            AddHighlightingWithBold(consumer, constructorDeclaration, tooltip);
        }

        public virtual void AddUnityImplicitFieldUsage(IHighlightingConsumer consumer, IFieldDeclaration field, string tooltip)
        {
            AddHighlightingWithBold(consumer, field, tooltip);
        }

        public virtual void AddUnityEventHandler(IHighlightingConsumer consumer, IDeclaration element, string tooltip)
        {
            if (element is ICSharpDeclaration cSharpDeclaration)
                AddHighlightingWithBold(consumer, cSharpDeclaration, tooltip);
        }

        public virtual void AddHighlightingWithBold(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip)
        {
            AddHighlighting(consumer, element, tooltip);
            AddBoldHighlighting(consumer, element, tooltip);
        }

        public virtual void AddBoldHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip)
        {
            if (SettingsStore.GetValue((UnitySettings key) => key.UnityHighlighterSchemeKind) == UnityHighlighterSchemeKind.None)
                return;
            
            consumer.AddHighlighting(new UnityImplicitBoldHighlighting(tooltip, element.NameIdentifier.GetDocumentRange()));
        }
        
        public virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip)
        {
            var mode = SettingsStore.GetValue((UnitySettings key) => key.UnityHighlighterSchemeKind);
            if (mode != UnityHighlighterSchemeKind.Gutter && mode != UnityHighlighterSchemeKind.CodeInsights) // if code insights disabled, show gutter icons
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
                    var isCoroutine = IsCoroutine(methodDeclaration, unityApi);
                    if (isCoroutine.HasValue)
                    {
                        IBulbAction bulbAction;
                        if (isCoroutine.Value)
                            bulbAction = new ConvertFromCoroutineBulbAction(methodDeclaration);
                        else
                            bulbAction = new ConvertToCoroutineBulbAction(methodDeclaration);
                        return new[]
                        {
                            new BulbMenuItem(new IntentionAction.MyExecutableProxi(bulbAction, Solution, textControl),
                                bulbAction.Text, BulbThemedIcons.ContextAction.Id,
                                BulbMenuAnchors.FirstClassContextItems)
                        };
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
    }
}