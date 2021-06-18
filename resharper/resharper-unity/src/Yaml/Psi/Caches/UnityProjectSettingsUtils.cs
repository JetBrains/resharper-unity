using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
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

        [CanBeNull]
        public static T GetSceneCollection<T>([CanBeNull] IYamlFile file)
            where T : class, INode
        {
            return file.GetUnityObjectPropertyValue<T>("EditorBuildSettings", "m_Scenes");
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