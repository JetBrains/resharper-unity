using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class ProjectExtensions
    {
        public const string AssetsFolder = "Assets";
        public const string ProjectSettingsFolder = "ProjectSettings";

        public static bool HasUnityReference([NotNull] this ISolution solution)
        {
            var tracker = solution.GetComponent<UnityReferencesTracker>();
            return tracker.HasUnityReference.Value;
        }

        /// <summary>
        ///  Checks that specific project unity reference or specific unity guid
        /// </summary>
        public static bool IsUnityProject([CanBeNull] this IProject project)
        {
            if (project == null || !project.IsValid())
                return false;
            // Only VSTU adds the Unity project flavour. Unity + Rider don't, so we have to look at references
            if (HasUnityFlavour(project))
                return true;

            var referenceTracker = project.GetComponent<UnityReferencesTracker>();
            return referenceTracker.IsUnityProject(project);
        }

        public static bool IsUnityGeneratedProject([CanBeNull] this IProject project)
        {
            if (project == null)
                return false;
            
            // This works for Assets for local Packages folders and for 'file:' based packages
            if (project.ProjectFileLocation.IsAbsolute)
            {
                return project.ProjectFileLocation.Directory.Combine(AssetsFolder).ExistsDirectory && IsUnityProject(project);
            }
            // for our tests // todo: refactor tests so they also check logic below
            return project.HasSubItems(AssetsFolder) && IsUnityProject(project);
        }
        public static bool HasUnityFlavour([CanBeNull] this IProject project)
        {
            return project != null && project.HasFlavour<UnityProjectFlavor>();
        }
    }
}