using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.TextControl;
using JetBrains.UI.BulbMenu;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [StaticSeverityHighlighting(Severity.INFO, HighlightingGroupIds.GutterMarksGroup,
        OverlapResolve = OverlapResolveKind.NONE, AttributeId = UnityHighlightingAttributeIds.UNITY_GUTTER_ICON_ATTRIBUTE)]
    public class UnityMarkOnGutter : IHighlighting, IUnityHighlighting
    {
        private readonly UnityApi myUnityApi;
        private readonly ITreeNode myElement;
        private readonly DocumentRange myRange;

        public UnityMarkOnGutter(UnityApi unityApi, ITreeNode element, DocumentRange range, string tooltip)
        {
            myUnityApi = unityApi;
            myElement = element;
            myRange = range;
            ToolTip = tooltip;
        }

        public bool IsValid()
        {
            return myElement == null || myElement.IsValid();
        }

        public DocumentRange CalculateRange() => myRange;
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;

        public IEnumerable<BulbMenuItem> GetBulbMenuItems(ISolution solution, ITextControl textControl)
        {
            var classDeclaration = myElement as IClassLikeDeclaration;
            if (classDeclaration != null)
            {
                var fix = new GenerateUnityEventFunctionsFix(classDeclaration);
                return new[]
                {
                    new BulbMenuItem(new IntentionAction.MyExecutableProxi(fix, solution, textControl),
                        "Generate Unity event functions", PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id,
                        BulbMenuAnchors.FirstClassContextItems)
                };
            }

            var methodDeclaration = myElement as IMethodDeclaration;
            if (methodDeclaration != null)
            {
                var isCoroutine = IsCoroutine(methodDeclaration, myUnityApi);
                if (isCoroutine.HasValue)
                {
                    IBulbAction bulbAction;
                    if (isCoroutine.Value)
                        bulbAction = new ConvertFromCoroutineBulbAction(methodDeclaration);
                    else
                        bulbAction = new ConvertToCoroutineBulbAction(methodDeclaration);
                    return new[]
                    {
                        new BulbMenuItem(new IntentionAction.MyExecutableProxi(bulbAction, solution, textControl),
                            bulbAction.Text, BulbThemedIcons.ContextAction.Id, BulbMenuAnchors.FirstClassContextItems)
                    };
                }
            }

            return EmptyList<BulbMenuItem>.Enumerable;
        }

        private static bool? IsCoroutine(IMethodDeclaration methodDeclaration, UnityApi unityApi)
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