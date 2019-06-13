using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{

    [SolutionComponent]
    public class FieldDetector : UnityDeclarationHighlightingProviderBase
    {
        private readonly UnityApi myUnityApi;

        public FieldDetector(ISolution solution, SolutionAnalysisService swa, SettingsStore settingsStore, PerformanceCriticalCodeCallGraphAnalyzer analyzer, UnityApi unityApi)
            : base(solution, swa, settingsStore, analyzer)
        {
            myUnityApi = unityApi;
        }
        
        public override IDeclaredElement Analyze(IDeclaration element, IHighlightingConsumer consumer, DaemonProcessKind kind)
        {
            if (!(element is IFieldDeclaration field))
                return null;

            var declaredElement = field.DeclaredElement;
            if (declaredElement == null)
                return null;

            bool isSerializedField = myUnityApi.IsSerialisedField(declaredElement);
            if (isSerializedField)
            {
                if (myUnityApi.IsDescendantOfMonoBehaviour(declaredElement.GetContainingType()))
                {
                    AddMonoBehaviourHighlighting(consumer, field, "Property", "This field is initialised from Inspector", kind);
                    return declaredElement;

                } else if (myUnityApi.IsDescendantOfScriptableObject(declaredElement.GetContainingType()))
                {
                    AddScriptableObjectHighlighting(consumer, field, "Property", "This field is initialised from Inspector", kind);
                    return declaredElement;

                } else if (myUnityApi.IsInjectedField(declaredElement))
                {
                    AddECSHighlighting(consumer, field, "Property", "This field is injected by Unity", kind);
                    return declaredElement;
                } else if (declaredElement.GetAttributeInstances(false)
                    .All(t => !t.GetClrName().Equals(KnownTypes.SerializeField)))
                {
                    AddSerializableHighlighting(consumer, field, "Serializable", "This field is serialized by Unity", kind);
                }

                return null;
            } 

            return null;
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
            consumer.AddHighlighting(new UnityGutterMarkInfo(element, tooltip));
        }

        protected override IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration)
        {
            return EnumerableCollection<BulbMenuItem>.Empty;
        }
    }
}