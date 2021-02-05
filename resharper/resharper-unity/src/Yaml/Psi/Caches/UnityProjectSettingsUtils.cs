using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public static class UnityProjectSettingsUtils
    {
        public static string GetUnityPathFor([NotNull] IPsiSourceFile psiSourceFile)
        {
            var solution = psiSourceFile.GetSolution();
            var solutionPath = solution.SolutionDirectory;
            var psiPath = psiSourceFile.GetLocation();
            var components = psiPath.MakeRelativeTo(solutionPath).Components.ToArray();

            var sb = new StringBuilder();
            // skip "Assets/

            for (int i = 1; i < components.Length - 1; i++)
            {
                sb.Append(components[i]);
                sb.Append('/');
            }

            sb.Append(psiPath.NameWithoutExtension);

            return sb.ToString();
        }

        public static INode GetSceneCollection([NotNull] IYamlFile file)
        {
            return GetCollection(file, "EditorBuildSettings", "m_Scenes");
        }

        public static INode GetCollection([CanBeNull] IYamlFile file, string documentName, string name)
        {
            var blockMappingNode = file?.Documents[0].Body.BlockNode as IBlockMappingNode;
            return GetCollection(blockMappingNode, documentName, name);
        }

        public static INode GetSceneCollection([CanBeNull] IBlockMappingNode blockMappingNode)
        {
            return GetCollection(blockMappingNode, "EditorBuildSettings", "m_Scenes");
        }

        public static INode GetCollection([CanBeNull] IBlockMappingNode blockMappingNode, string documentName, string name)
        {
            var documentEntry = blockMappingNode?.Entries.FirstOrDefault(
                t => documentName.Equals(t.Key.GetPlainScalarText()))?.Content.Value as IBlockMappingNode;

            var collection = documentEntry?.Entries.FirstOrDefault(t => name.Equals(t.Key.GetPlainScalarText()))?.Content.Value;

            return collection;
        }

        public static string GetUnityScenePathRepresentation(string scenePath)
        {
            return scenePath.RemoveStart("Assets/").RemoveEnd(UnityYamlFileExtensions.SceneFileExtensionWithDot);
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