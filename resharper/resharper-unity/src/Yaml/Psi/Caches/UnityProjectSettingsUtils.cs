using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public static class UnityProjectSettingsUtils
    {
        public static string GetUnityPathFor([NotNull] IPsiSourceFile psiSourceFile)
        {
            var solution = psiSourceFile.GetSolution();
            var solutionPath = solution.SolutionFilePath;
            var psiPath = psiSourceFile.GetLocation();
            var components = psiPath.MakeRelativeTo(solutionPath).Components.ToArray();

            var sb = new StringBuilder();
            // skip "../Assets/

            for (int i = 2; i < components.Length - 1; i++)
            {
                sb.Append(components[i]);
                sb.Append('/');
            }

            sb.Append(psiPath.NameWithoutExtension);

            return sb.ToString();
        }
        
        public static INode GetSceneCollection([NotNull] IYamlFile file)
        {
            var blockMappingNode = file.Documents[0].Body.BlockNode as IBlockMappingNode;
            Assertion.Assert(blockMappingNode != null, "blockMappingNode != null");

            return GetSceneCollection(blockMappingNode);
        }
        
        public static INode GetSceneCollection([NotNull] IBlockMappingNode blockMappingNode)
        {
            var editorBuildSettingEntry = blockMappingNode.Entries.FirstOrDefault(
                t => t.Key.GetText().Equals("EditorBuildSettings"))?.Value as IBlockMappingNode;
            Assertion.Assert(editorBuildSettingEntry != null, "editorBuildSettingEntry != null");

            var scenes = editorBuildSettingEntry.Entries.FirstOrDefault(t => t.Key.GetText().Equals("m_Scenes"))?.Value;
            Assertion.Assert(scenes != null, "scenes != null");
            
            return scenes;
        }

        public static string GetUnityScenePathRepresentation(string scenePath)
        {
            return scenePath.RemoveStart("Assets/").RemoveEnd(UnityYamlFileExtensions.SceneFileExtensionWithDot);
        }
        
                public static bool IsEditorSceneManagerSceneRelatedMethod(IInvocationExpressionReference reference)
        {
            return IsSceneRelatedMethod(reference, IsEditorSceneManagerLoadScene);
        }

        public static bool IsSceneManagerSceneRelatedMethod(IInvocationExpressionReference reference)
        {
            return IsSceneRelatedMethod(reference, IsSceneManagerLoadScene);
        }

        public static ICSharpArgument GetSceneNameArgument(IInvocationExpression invocationExpression)
        {
            return invocationExpression.ArgumentList.Arguments.FirstOrDefault
                (t => t.IsNamedArgument && t.ArgumentName?.Equals("sceneName") == true || !t.IsNamedArgument);
        }

        public static string GetScenePathFromArgument(ICSharpLiteralExpression literalExpression)
        {
            // There are 3 ways to present scene name in unity
            // Consider scene : Assets/Scenes/myScene.unity
            // User could use "myScene", "Scenes/myScene" and "Assets/Scenes/myScene.unity" to load scene
            // Internally, we work only with first and second format (see UnityProjectSettingsCache)

            var constantValue = literalExpression.ConstantValue.Value as string;
            if (constantValue == null)
                return null;

            var sceneName = constantValue;
            if (sceneName.StartsWith("Assets/") && sceneName.EndsWith(UnityYamlFileExtensions.SceneFileExtensionWithDot))
                sceneName = GetUnityScenePathRepresentation(sceneName);

            return sceneName;
        }
        
        public static bool IsSceneRelatedMethod(IInvocationExpressionReference reference, Func<IMethod, bool> checker)
        {
            var result = reference.Resolve();
            if (checker(result.DeclaredElement as IMethod))
                return true;
            
            foreach (var candidate in result.Result.Candidates)
            {
                if (checker(candidate as IMethod))
                    return true;
            }

            return false;
        }

        private static bool IsSceneManagerLoadScene(IMethod method)
        {
            if (method != null && method.ShortName.StartsWith("LoadScene") &&
                method.GetContainingType()?.GetClrName().Equals(KnownTypes.SceneManager) == true)
            {
                return true;
            }

            return false;
        }
        
        private static bool IsEditorSceneManagerLoadScene(IMethod method)
        {
            if (method != null && method.ShortName.Equals("OpenScene") &&
                method.GetContainingType()?.GetClrName().Equals(KnownTypes.EditorSceneManager) == true)
            {
                return true;
            }

            return false;
        }
        
        public static UnityExternalFilesPsiModule GetUnityModule(ISolution solution)
        {
            return solution.GetComponent<UnityExternalFilesModuleFactory>().PsiModule;
        }

        public static IPsiSourceFile GetEditorBuildSettings([CanBeNull]UnityExternalFilesPsiModule psiModule)
        {
            return psiModule?.SourceFiles.FirstOrDefault(t => t.Name.Equals("EditorBuildSettings.asset"));
        }
    }
}