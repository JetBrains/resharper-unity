using JetBrains.Metadata.Utils;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours;
using JetBrains.Util.Reflection;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class ProjectExtensions
    {
        private static readonly AssemblyNameInfo ourUnityEngineReferenceName = AssemblyNameInfoFactory.Create2("UnityEngine", null);
        private static readonly AssemblyNameInfo ourUnityEditorReferenceName = AssemblyNameInfoFactory.Create2("UnityEditor", null);

        public static bool IsUnityProject(this IProject project)
        {
            return project.HasFlavour<UnityProjectFlavor>() || ReferencesUnity(project);
        }

        private static bool ReferencesUnity(IProject project)
        {
            return ReferencesAssembly(project, ourUnityEngineReferenceName) ||
                   ReferencesAssembly(project, ourUnityEditorReferenceName);
        }

        private static bool ReferencesAssembly(IProject project, AssemblyNameInfo name)
        {
            AssemblyNameInfo info;
            return ReferencedAssembliesService.IsProjectReferencingAssemblyByName(project,
                project.GetCurrentTargetFrameworkId(), name, out info);
        }
    }
}