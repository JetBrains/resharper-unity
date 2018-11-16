using System;
using JetBrains.Annotations;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public class UnityEditorOccurrence : ReferenceOccurrence
    {
        private readonly UnityEventTargetReference myUnityEventTargetReference;
        private UnityHost myUnityHost;
        public UnityEditorOccurrence([NotNull] ITreeNode treeNode, UnityEventTargetReference unityEventTargetReference,
            OccurrenceType occurrenceType)
            : base(treeNode, occurrenceType)
        {
            myUnityEventTargetReference = unityEventTargetReference;
            myUnityHost = treeNode.GetSolution().GetComponent<UnityHost>();
        }

        public override bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            var result = UnityObjectPsiUtil.GetGameObjectPath(myUnityEventTargetReference.ComponentDocument);
            myUnityHost.PerformModelAction(t => t.ShowGameObjectOnScene.Set(result));
            return false;
        }
    }
}