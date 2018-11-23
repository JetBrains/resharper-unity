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
            var gameObjectDocument = currentReference.ComponentDocument.GetUnityObjectDocumentFromFileIDProperty("m_GameObject");

            var sourceFile = gameObjectDocument?.GetSourceFile();
            if (sourceFile == null)
                return null;
            
            var pathElements = UnityObjectPsiUtil.ConstructGameObjectPath(gameObjectDocument, (document, isPrefab) =>
            {
                IYamlDocument parent;
                string name;
                if (isPrefab)
                {
                    name = UnityObjectPsiUtil.ExtractNameFromPrefab(gameObjectDocument);
                    parent = UnityObjectPsiUtil.GetParentFromPrefab(gameObjectDocument);
                }
                else
                {
                    name = document.GetUnityObjectPropertyValue("m_Name").AsString();
                    parent = UnityObjectPsiUtil.GetParentGameObject(gameObjectDocument);
                }
                
                return new PathElement(name, GetChildIndex(gameObjectDocument.GetFileId(), parent));
            });

            
            var pathFromAsset = GetPathFromAssetFolder(sourceFile, out var extension);
            bool needExpand = currentReference == selectedReference;
          
            if (extension.Equals("prefab", StringComparison.OrdinalIgnoreCase))
            {
                return new FindUsageRequestPrefab(needExpand, pathFromAsset, new String[]{});
            }
            
            if (extension.Equals("unity", StringComparison.OrdinalIgnoreCase))
            {
                var localId = gameObjectDocument.GetFileId();
                if (localId == null)
                    return null;
                return new FindUsageRequestScene(localId, needExpand, pathFromAsset, new String[]{});
            }
            
            return null;
        }

        private int GetChildIndex(string getFileId, IYamlDocument parent)
        {
            var transform = UnityObjectPsiUtil.FindTransformComponentForGameObject(parent);
            var children = transform?.GetUnityObjectPropertyValue("m_Children") as IBlockSequenceNode;
            if (children == null)
                return -1;
            int result = 0;
            foreach (var componentEntry in children.Entries)
            {
                
                return result;
            }
            return -1;
        }

        private string GetPathFromAssetFolder([NotNull] IPsiSourceFile file, out string extension)
        {
            extension = null;
            var path = file.GetLocation().MakeRelativeTo(solutionDirectoryPath);
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

    internal class PathElement
    {
        public PathElement(string name, object getChildIndex)
        {
            throw new NotImplementedException();
        }
    }
}