using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
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
            var declaredElement = myUnityEventTargetReference.Resolve().DeclaredElement;
            if (declaredElement == null)
                return false;

            var request = CreateFindUsageRequest(myUnityEventTargetReference);
            if (request == null)
                return false;
           
            var pid = myUnityHost.GetValue(m => m.UnityProcessId.Value);
            UnityFocusUtil.FocusUnity(pid);
            
            myUnityHost.PerformModelAction(t => t.ShowGameObjectOnScene.Set(request));
            PushAllUnityUsagesToUnity(myUnityHost, solution.GetPsiServices().Finder, declaredElement);
            return false;
        }

        private void PushAllUnityUsagesToUnity(UnityHost unityHost, IFinder finder, IDeclaredElement declaredElement)
        {
            var results = finder.FindReferences(declaredElement, declaredElement.GetSearchDomain(), NullProgressIndicator.Create());
            var findUsageRequests = new List<FindUsageRequest>();
            foreach (var reference in results)
            {
                if (reference is UnityEventTargetReference unityEventTargetReference)
                {
                    var result = CreateFindUsageRequest(unityEventTargetReference);
                    if (result != null)
                        findUsageRequests.Add(result);
                }
            }
            
            unityHost.PerformModelAction(t => t.FindUsageResult.Set(findUsageRequests.ToArray()));
        }

        private static FindUsageRequest CreateFindUsageRequest(UnityEventTargetReference reference)
        {
            var result = UnityObjectPsiUtil.GetGameObjectPath(reference.ComponentDocument);

            var componentDocument = reference.ComponentDocument;
            var gameObjectDocument = componentDocument.GetUnityObjectDocumentFromFileIDProperty("m_GameObject");

            var blockNode = gameObjectDocument?.BlockNode as IBlockMappingNode;
            if (blockNode == null)
                return null;

            if (!int.TryParse(blockNode.Properties.AnchorProperty.Text.GetText(), out var localId))
                return null;

            var yamlFileName = componentDocument.GetSourceFile().NotNull("componentDocument.GetSourceFile() != null").Name;
            var elements = yamlFileName.Split(".");
            var fileName = elements[0];
            var extension = elements[1]; // TODO handle prefabs
            
            return new FindUsageRequest(localId,fileName, result.RemoveEnd("\\").Split("\\"));
        }
    }
}