using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    [SolutionComponent]
    public class FieldDetector : UnityDeclarationHighlightingProviderBase
    {
        private readonly UnityApi myUnityApi;

        public FieldDetector(ISolution solution,
                             IApplicationWideContextBoundSettingStore settingsStore,
                             UnityApi unityApi, 
                             PerformanceCriticalContextProvider contextProvider)
            : base(solution, settingsStore, contextProvider)
        {
            myUnityApi = unityApi;
        }

        public override bool AddDeclarationHighlighting(IDeclaration element, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (element is not ICSharpDeclaration cSharpDeclaration)
                return false;

            var declaredElement = cSharpDeclaration.DeclaredElement;

            var isSerializedField = false;
            ITypeElement containingType = null;

            switch (declaredElement)
            {
                case IField field when
                    myUnityApi.IsSerialisedField(field) == SerializedFieldStatus.SerializedField:
                    isSerializedField = true;
                    containingType = field.ContainingType;
                    break;
                case IProperty property when myUnityApi.IsSerialisedAutoProperty(property, useSwea: true) == SerializedFieldStatus.SerializedField:
                    isSerializedField = true;
                    containingType = property.ContainingType;
                    break;
            }

            if (!isSerializedField) 
                return false;
            
            var displayText = Strings.FieldDetector_AddDeclarationHighlighting_Serializable;
            
            if (containingType.DerivesFromMonoBehaviour() || containingType.DerivesFromScriptableObject())
            {
                AddMonoBehaviourHighlighting(consumer, cSharpDeclaration, displayText, Strings.FieldDetector_AddDeclarationHighlighting_This_field_is_initialized_from_Inspector, context);
                return true;
            }

            AddSerializableHighlighting(consumer, cSharpDeclaration, displayText, Strings.FieldDetector_AddDeclarationHighlighting_Tooltip, context);
            return false;

        }

        protected virtual void AddMonoBehaviourHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
            IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, element, text, tooltip, context);
        }

        protected virtual void AddSerializableHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
            IReadOnlyCallGraphContext context)
        {
            AddHighlighting(consumer, element, text, tooltip, context);
        }

        protected override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string text, string tooltip,
            IReadOnlyCallGraphContext context)
        {
            consumer.AddImplicitConfigurableHighlighting(element);
            
            if (!IconProviderUtil.ShouldShowGutterMarkIcon(SettingsStore.BoundSettingsStore))
                return;
            
            consumer.AddHighlighting(new UnityGutterMarkInfo(element, tooltip));
        }

        protected override IEnumerable<BulbMenuItem> GetActions(ICSharpDeclaration declaration)
        {
            return EnumerableCollection<BulbMenuItem>.Empty;
        }
    }
}