using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Rider.Model;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityEditorFindRequestCreator
    {
        private readonly ISolution mySolution;
        private readonly UnityHost myUnityHost;
        private readonly FileSystemPath solutionDirectoryPath;

        public UnityEditorFindRequestCreator(ISolution solution, UnityHost unityHost)
        {
            mySolution = solution;
            myUnityHost = unityHost;
            solutionDirectoryPath = solution.SolutionDirectory;
        }

        public bool CreateRequestToUnity(IUnityYamlReference yamlReference, bool focusUnity)
        {
            var declaredElement = yamlReference.Resolve().DeclaredElement;
            if (declaredElement == null)
                return false;

            var finder = mySolution.GetPsiServices().Finder;
            var references = finder.FindReferences(declaredElement, declaredElement.GetSearchDomain(), NullProgressIndicator.Create())
                .Where(t => t is IUnityYamlReference);

            var result = new List<FindUsageRequestBase>();

            foreach (var reference in references)
            {
                var request = CreateRequest((reference as IUnityYamlReference).NotNull("currentReference != null"), yamlReference);
                if (request != null)
                    result.Add(request);
            }

            if (result.Count == 0)
                return false;

            if (focusUnity)
            {
                UnityFocusUtil.FocusUnity(myUnityHost.GetValue(t => t.UnityProcessId.Value));
            }
            
            myUnityHost.PerformModelAction(t => t.ShowGameObjectOnScene.Set(CreateRequest(yamlReference, null)));
            myUnityHost.PerformModelAction(t => t.FindUsageResult.Set(result.ToArray()));
            return true;
        }

        private FindUsageRequestBase CreateRequest([NotNull] IUnityYamlReference currentReference, [CanBeNull] IUnityYamlReference selectedReference)
        {
            var componentDocument = currentReference.ComponentDocument; 
            var gameObjectPath = UnityObjectPsiUtil.GetGameObjectPath(componentDocument);
            var pathElements = gameObjectPath.RemoveEnd("\\").Split("\\");
            
            var gameObjectDocument = componentDocument.GetUnityObjectDocumentFromFileIDProperty("m_GameObject");
            var blockNode = gameObjectDocument?.BlockNode as IBlockMappingNode;
            if (blockNode == null)
                return null;
            
            var pathFromAsset = GetPathFromAssetFolder(blockNode, out var extension);

            bool needExpand = currentReference == selectedReference;
          
            if (extension.Equals("prefab", StringComparison.OrdinalIgnoreCase))
            {
                return new FindUsageRequestPrefab(needExpand, pathFromAsset, pathElements);
            }
            
            if (extension.Equals("unity", StringComparison.OrdinalIgnoreCase))
            {
                var localId = GetLocalId(blockNode);
                if (localId == null)
                    return null;
                return new FindUsageRequestScene(localId, needExpand, pathFromAsset, pathElements);
            }
            
            return null;
        }
 
        private string GetLocalId([NotNull] IBlockMappingNode blockNode)
        {
            return blockNode.Properties.AnchorProperty.Text.GetText();
        }

        private string GetPathFromAssetFolder([NotNull] IBlockMappingNode blockNode, out string extension)
        {
            extension = null;
            var path = blockNode.GetSourceFile().GetLocation().MakeRelativeTo(solutionDirectoryPath);
            var assetFolder = path.FirstComponent;
            if (!assetFolder.Equals("Assets"))
                return null;
            
            var pathComponents = path.GetPathComponents();

            var sb = new StringBuilder();
            for (int i = 0; i < pathComponents.Length; i++)
            {
                if (i + 1 == pathComponents.Length)
                {
                    var name = path.NameWithoutExtension;
                    sb.Append(name);
                    extension = pathComponents[i].RemoveStart(name + ".");
                    break;
                }

                sb.Append(pathComponents[i]);
                sb.Append('/');
            }
            
            return sb.ToString();
        }
    }
}