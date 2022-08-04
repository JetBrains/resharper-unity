using System;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util.Extension;
using ProjectExtensions = JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.ProjectExtensions;

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
        
        [CanBeNull]
        public static T GetValue<T>([CanBeNull] IYamlFile file, [NotNull] string objectType, [NotNull] string key)
            where T : class, INode
        {
            return file.GetUnityObjectPropertyValue<T>(objectType, key);
        }

        public static string GetUnityScenePathRepresentation(string scenePath)
        {
            return scenePath.RemoveStart("Assets/").RemoveEnd(UnityFileExtensions.SceneFileExtensionWithDot,
                StringComparison.InvariantCultureIgnoreCase);
        }

        public static UnityExternalFilesPsiModule GetUnityModule(ISolution solution)
        {
            return solution.GetComponent<UnityExternalFilesModuleFactory>().PsiModule;
        }

        public static IPsiSourceFile GetEditorBuildSettings([CanBeNull]UnityExternalFilesPsiModule psiModule)
        {
            return GetProjectSettingsPsiSourceFile(psiModule, "EditorBuildSettings.asset");
        }

        public static IPsiSourceFile GetEditorSettings(UnityExternalFilesPsiModule psiModule)
        {
            return GetProjectSettingsPsiSourceFile(psiModule, "EditorSettings.asset");
        }

        private static IPsiSourceFile GetProjectSettingsPsiSourceFile(UnityExternalFilesPsiModule psiModule,
            string fileName)
        {
            if (psiModule == null)
                return null;
            var solutionDir = psiModule.GetSolution().SolutionDirectory;
            return psiModule.TryGetFileByPath(
                solutionDir.Combine(ProjectExtensions.ProjectSettingsFolder).Combine(fileName),
                out var file)
                ? file
                : null;
        }
    }
}