using System;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.SerializationAttributes
{
    [DaemonStage(Instantiation.DemandAnyThreadUnsafe, GlobalAnalysisStage = true, StagesBefore = [typeof(SolutionAnalysisFileStructureCollectorStage)])]
    public class UnitySerializationAttributeGlobalStage : CSharpDaemonStageBase
    {
        private readonly UnityApi myUnityApi;
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public UnitySerializationAttributeGlobalStage(UnityApi unityApi, UnitySolutionTracker unitySolutionTracker)
        {
            myUnityApi = unityApi;
            myUnitySolutionTracker = unitySolutionTracker;
        }

        protected override bool IsSupported(IPsiSourceFile sourceFile)
        {
            return /*sourceFile.GetProject().IsUnityProject()*/
                myUnitySolutionTracker.HasUnityReference.Value
                && base.IsSupported(sourceFile);
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            return new SerializeReferenceHighlightingProcess(process, file, settings, myUnityApi);
        }
    }


    public class SerializeReferenceHighlightingProcess : CSharpDaemonStageProcessBase
    {
        private readonly IContextBoundSettingsStore mySettingsStore;
        private readonly UnityApi myUnityApi;

        public SerializeReferenceHighlightingProcess([NotNull] IDaemonProcess process, [NotNull] ICSharpFile file,
            IContextBoundSettingsStore settingsStore, UnityApi unityApi) :
            base(process, file)
        {
            mySettingsStore = settingsStore;
            myUnityApi = unityApi;
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            HighlightInFile((file, consumer) => file.ProcessDescendants(this, consumer), committer, mySettingsStore);
        }

        public override void ProcessBeforeInterior(ITreeNode element, IHighlightingConsumer consumer)
        {
            if (element is not IAttribute attribute)
                return;
            Analyze(attribute, consumer);
        }

        private void Analyze(IAttribute attribute, IHighlightingConsumer consumer)
        {
            if (!(attribute.TypeReference?.Resolve().DeclaredElement is ITypeElement attributeTypeElement))
                return;

            if (!Equals(attributeTypeElement.GetClrName(), KnownTypes.SerializeField))
                return;

            foreach (var declaration in AttributesOwnerDeclarationNavigator.GetByAttribute(attribute))
            {
                if (declaration.DeclaredElement is IField field
                    && myUnityApi.IsSerialisedField(field, false).HasFlag(SerializedFieldStatus.Unknown) //if we don't have info on the local state
                    && myUnityApi.IsSerialisedField(field).HasFlag(SerializedFieldStatus.NonSerializedField)

                    || (declaration.DeclaredElement is IProperty property
                        && attribute.Target == AttributeTarget.Field
                        && myUnityApi.IsSerialisedAutoProperty(property, false).HasFlag(SerializedFieldStatus.Unknown)
                        && myUnityApi.IsSerialisedAutoProperty(property, true).HasFlag(SerializedFieldStatus.NonSerializedField))
                    )
                {
                    //TODO - only for previously unknown types
                    consumer.AddHighlighting(new RedundantSerializeFieldAttributeWarning(attribute));
                    return;
                }
            }
        }
    }
}
