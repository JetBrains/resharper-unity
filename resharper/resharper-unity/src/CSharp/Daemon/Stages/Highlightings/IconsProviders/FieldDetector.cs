using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{

    [SolutionComponent]
    public class FieldDetector : UnityDeclarationHighlightingProviderBase
    {
        private readonly UnityApi myUnityApi;

        public FieldDetector(ISolution solution, CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, SettingsStore settingsStore, PerformanceCriticalCodeCallGraphMarksProvider marksProvider, UnityApi unityApi, IElementIdProvider provider)
            : base(solution, callGraphSwaExtensionProvider, settingsStore, marksProvider, provider)
        {
            myUnityApi = unityApi;
        }
        
        public override bool AddDeclarationHighlighting(IDeclaration element, IHighlightingConsumer consumer, DaemonProcessKind kind)
        {
            if (!(element is IFieldDeclaration field))
                return false;

            var declaredElement = field.DeclaredElement;
            if (declaredElement == null)
                return false;

            bool isSerializedField = myUnityApi.IsSerialisedField(declaredElement);
            if (isSerializedField)
            {
                const string displayText = "Serializable";
                const string baseTooltip = "This field is initialized from Inspector";
                if (UnityApi.IsDescendantOfMonoBehaviour(declaredElement.GetContainingType()) ||
                    UnityApi.IsDescendantOfScriptableObject(declaredElement.GetContainingType()))
                {
                    AddMonoBehaviourHighlighting(consumer, field, displayText, baseTooltip, kind);
                    return true;

                } else if (myUnityApi.IsInjectedField(declaredElement))
                {
                    AddECSHighlighting(consumer, field, displayText, "This field is injected by Unity", kind);
                    return true;
                } else 
                {
                    AddSerializableHighlighting(consumer, field, displayText, "This field is serialized by Unity", kind);
                }

                return false;
            } 

            return false;
        }

        protected virtual void AddMonoBehaviourHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
            DaemonProcessKind kind)
        {
            AddHighlighting(consumer, element, text, tooltip, kind);
        }
        
        protected virtual void AddScriptableObjectHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
            DaemonProcessKind kind)
        {
            AddHighlighting(consumer, element, text, tooltip, kind);
        }
        
        protected virtual void AddECSHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
            DaemonProcessKind kind)
        {
            AddHighlighting(consumer, element, text, tooltip, kind);
        }
        
        protected virtual void AddSerializableHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
            DaemonProcessKind kind)
        {
            AddHighlighting(consumer, element, text, tooltip, kind);
        }
        
        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
            DaemonProcessKind kind)
        {
            consumer.AddImplicitConfigurableHighlighting(element);
            consumer.AddHighlighting(new UnityGutterMarkInfo(element, tooltip));
        }

        protected override IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration)
        {
            return EnumerableCollection<BulbMenuItem>.Empty;
        }
    }
}