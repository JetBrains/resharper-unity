using System.Collections.Generic;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Resources.Icons;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class TypeDetector : UnityDeclarationHighlightingProviderBase
    {
        private readonly UnityApi myUnityApi;

        public TypeDetector(ISolution solution, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, SettingsStore settingsStore, UnityApi unityApi,
            PerformanceCriticalCodeMarksProvider marksProvider, IElementIdProvider provider)
            : base(solution, callGraphSwaExtensionProvider, settingsStore, marksProvider, provider)
        {
            myUnityApi = unityApi;
        }

        public override bool AddDeclarationHighlighting(IDeclaration node, IHighlightingConsumer consumer, DaemonProcessKind kind)
        {
            if (!(node is IClassLikeDeclaration element))
                return false;

            var typeElement = element.DeclaredElement;
            if (typeElement != null)
            {
                if (typeElement.DerivesFromMonoBehaviour())
                {
                    AddMonoBehaviourHiglighting(consumer, element, "Script", "Unity script", kind);
                }
                else if (typeElement.DerivesFrom(KnownTypes.Editor) || typeElement.DerivesFrom(KnownTypes.EditorWindow))
                {
                    AddEditorHiglighting(consumer, element, "Editor", "Custom Unity Editor", kind);
                }
                else if (typeElement.DerivesFromScriptableObject())
                {
                    AddMonoBehaviourHiglighting(consumer, element, "Scriptable object", "Scriptable Object", kind);
                }
                else if (myUnityApi.IsUnityType(typeElement))
                {
                    AddUnityTypeHighlighting(consumer, element, "Unity type", "Custom Unity type", kind);
                }
                else if (myUnityApi.IsUnityECSType(typeElement))
                {
                    AddUnityECSHighlighting(consumer, element, "Unity ECS", "Unity entity component system object",
                        kind);
                }

                return true;
            }

            return false;
        }

        protected virtual void AddMonoBehaviourHiglighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text, string tooltip, DaemonProcessKind kind)
        {
            AddHighlighting(consumer, declaration, text, tooltip, kind);
        }

        protected virtual void AddEditorHiglighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text, string tooltip, DaemonProcessKind kind)
        {
            AddHighlighting(consumer, declaration, text, tooltip, kind);
        }

        protected virtual void AddUnityTypeHighlighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text, string tooltip, DaemonProcessKind kind)
        {
            AddHighlighting(consumer, declaration, text, tooltip, kind);
        }

        protected virtual void AddUnityECSHighlighting(IHighlightingConsumer consumer, IClassLikeDeclaration declaration, string text, string tooltip, DaemonProcessKind kind)
        {
            AddHighlighting(consumer, declaration, text, tooltip, kind);
        }


        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration declaration, string text, string tooltip, DaemonProcessKind kind)
        {
            consumer.AddImplicitConfigurableHighlighting(declaration);
            consumer.AddHighlighting(new UnityGutterMarkInfo(GetActions(declaration), declaration, tooltip));
        }

        protected override IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration)
        {
            var result = new List<BulbMenuItem>();
            var textControl = Solution.GetComponent<ITextControlManager>().LastFocusedTextControl.Value;
            if (declaration is IClassLikeDeclaration classLikeDeclaration &&
                textControl != null && myUnityApi.IsUnityType(classLikeDeclaration.DeclaredElement))
            {
                var fix = new GenerateUnityEventFunctionsFix(classLikeDeclaration);
                result.Add(
                    new BulbMenuItem(new IntentionAction.MyExecutableProxi(fix, Solution, textControl),
                        "Generate Unity event functions", PsiFeaturesUnsortedThemedIcons.FuncZoneGenerate.Id,
                        BulbMenuAnchors.FirstClassContextItems)
                );
            }

            return result;
        }
    }
}