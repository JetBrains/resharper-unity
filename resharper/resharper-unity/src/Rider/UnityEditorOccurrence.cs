using System;
using System.Runtime.InteropServices;
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
using JetBrains.ReSharper.Psi.Xaml.Impl.Resolve;
using JetBrains.Rider.Model;
using JetBrains.Util;
using JetBrains.Util.Extension;

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
            var pid = myUnityHost.GetValue(m => m.UnityProcessId.Value);
            UnityFocusUtil.FocusUnity(pid);
            
            var result = UnityObjectPsiUtil.GetGameObjectPath(myUnityEventTargetReference.ComponentDocument);

            var componentDocument = myUnityEventTargetReference.ComponentDocument;
            var gameObjectDocument = componentDocument.GetUnityObjectDocumentFromFileIDProperty("m_GameObject");

            var blockNode = gameObjectDocument?.BlockNode as IBlockMappingNode;
            if (blockNode == null)
                return false;

            if (!int.TryParse(blockNode.Properties.AnchorProperty.Text.GetText(), out var localId))
                return false;

            var yamlFileName = componentDocument.GetSourceFile().NotNull("componentDocument.GetSourceFile() != null").Name;
            var elements = yamlFileName.Split(".");
            var fileName = elements[0];
            var extension = elements[1];
            
            var request = new FindUsageRequest(localId,fileName, result.RemoveEnd("\\").Split("\\"));
            myUnityHost.PerformModelAction(t => t.ShowGameObjectOnScene.Set(request));
            return false;
        }

        private void PushAllUsagesToUnity()
        {
            
        }
    }
}