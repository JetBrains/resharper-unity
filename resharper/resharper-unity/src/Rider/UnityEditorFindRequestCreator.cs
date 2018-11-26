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
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Rider.Model;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityEditorFindRequestCreator
    {
        private readonly ISolution mySolution;
        private readonly UnityHost myUnityHost;
        private readonly FileSystemPath mySolutionDirectoryPath;

        public UnityEditorFindRequestCreator(ISolution solution, UnityHost unityHost)
        {
            mySolution = solution;
            myUnityHost = unityHost;
            mySolutionDirectoryPath = solution.SolutionDirectory;
        }

        public bool CreateRequestToUnity([NotNull] IUnityYamlReference yamlReference, bool focusUnity)
        {
            var declaredElement = yamlReference.Resolve().DeclaredElement;
            if (declaredElement == null)
                return false;
            
            return CreateRequestToUnity(declaredElement, yamlReference, focusUnity);
        }

        public bool CreateRequestToUnity([NotNull] IDeclaredElement declaredElement, [CanBeNull] IUnityYamlReference selectedReference, bool focusUnity)
        {
            var finder = mySolution.GetPsiServices().Finder;
            var references = finder.FindReferences(declaredElement, declaredElement.GetSearchDomain(), NullProgressIndicator.Create())
                .Where(t => t is IUnityYamlReference);

            var result = new List<FindUsageRequest>();

            foreach (var reference in references)
            {
                var request = CreateRequest((reference as IUnityYamlReference).NotNull("currentReference != null"), selectedReference);
                if (request != null)
                    result.Add(request);
            }

            if (result.Count == 0)
                return false;

            if (focusUnity)
            {
                UnityFocusUtil.FocusUnity(myUnityHost.GetValue(t => t.UnityProcessId.Value));
            }
            
            if (selectedReference != null)
                myUnityHost.PerformModelAction(t => t.ShowGameObjectOnScene.Set(CreateRequest(selectedReference, null)));
            myUnityHost.PerformModelAction(t => t.FindUsageResult.Set(result.ToArray()));
            return true;
        }
        
        private FindUsageRequest CreateRequest([NotNull] IUnityYamlReference currentReference, [CanBeNull] IUnityYamlReference selectedReference)
        {
            var gameObjectDocument = currentReference.ComponentDocument.GetUnityObjectDocumentFromFileIDProperty("m_GameObject") ?? currentReference.ComponentDocument;

            var sourceFile = gameObjectDocument?.GetSourceFile();
            if (sourceFile == null)
                return null;

            var pathElements = UnityObjectPsiUtil.GetGameObjectPathFromComponent(currentReference.ComponentDocument).RemoveEnd("\\").Split("\\");
            var rootIndices = new FrugalLocalList<int>();
            
            // Constructing path of child indices
            UnityObjectPsiUtil.ProcessToRoot(gameObjectDocument,(currentGameObject, modification) =>
                {
                    int rootOrder = -1;
                    var transform = UnityObjectPsiUtil.FindTransformComponentForGameObject(currentGameObject);
                    if (modification != null)
                    {
                        if (!int.TryParse(UnityObjectPsiUtil.GetValueFromModifications(modification, transform.GetFileId(), "m_RootOrder")
                            , out rootOrder))
                            rootOrder = -1;
                    }
                    if (rootOrder == -1)
                    {
                        var rootOrderAsString = transform.GetUnityObjectPropertyValue("m_RootOrder").AsString();
                        if (!int.TryParse(rootOrderAsString, out rootOrder))
                            rootOrder = -1;
                    }
                    rootIndices.Add(rootOrder);
                });

            
            var pathFromAsset = GetPathFromAssetFolder(sourceFile, out var extension);
            bool needExpand = currentReference == selectedReference;
            bool isPrefab = extension.Equals("prefab", StringComparison.OrdinalIgnoreCase);

            var rootIndicesReversed = new int[rootIndices.Count];
            for (int i = 0; i < rootIndices.Count; i++)
                rootIndicesReversed[i] = rootIndices[rootIndices.Count - 1 - i];
            
            return new FindUsageRequest(isPrefab, needExpand, pathFromAsset, pathElements, rootIndicesReversed);
        }

        private string GetPathFromAssetFolder([NotNull] IPsiSourceFile file, out string extension)
        {
            extension = null;
            var path = file.GetLocation().MakeRelativeTo(mySolutionDirectoryPath);
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