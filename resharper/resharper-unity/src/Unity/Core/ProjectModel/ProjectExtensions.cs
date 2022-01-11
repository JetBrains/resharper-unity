using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Properties.Flavours;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel
{
    public static class ProjectExtensions
    {
        public const string AssetsFolder = "Assets";
        public const string PackagesFolder = "Packages";
        public const string ProjectSettingsFolder = "ProjectSettings";
        public const string LibraryFolder = "Library";

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

            var referenceTracker = project.GetData(UnityReferencesTracker.UnityReferencesTrackerKey);
            if (referenceTracker == null)
                return false;

            return referenceTracker.IsUnityProject(project);
        }

        public static bool IsPlayerProject([CanBeNull] this IProject project)
        {
            if (project == null || !project.IsValid())
                return false;
            return project.Name.EndsWith(".Player");
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
            // for our tests // todo: refactor tests so they also check logic above
            return project.HasSubItems(AssetsFolder) && IsUnityProject(project);
        }

        public static bool HasUnityFlavour([CanBeNull] this IProject project)
        {
            return project != null && project.HasFlavour<UnityProjectFlavor>();
        }

        public static bool IsOneOfPredefinedUnityProjects([CanBeNull] this IProject project)
        {
            return project != null && project.IsAssemblyCSharp() || project.IsAssemblyCSharpEditor() ||
                   project.IsAssemblyCSharpFirstpass() || project.IsAssemblyCSharpFirstpassEditor();
        }

        public static bool IsAssemblyCSharp([CanBeNull] this IProject project)
        {
            return project != null && project.Name == "Assembly-CSharp";
        }

        public static bool IsAssemblyCSharpEditor([CanBeNull] this IProject project)
        {
            return project != null && project.Name == "Assembly-CSharp-Editor";
        }

        public static bool IsAssemblyCSharpFirstpass([CanBeNull] this IProject project)
        {
            return project != null && project.Name == "Assembly-CSharp-firstpass";
        }

        public static bool IsAssemblyCSharpFirstpassEditor([CanBeNull] this IProject project)
        {
            return project != null && project.Name == "Assembly-CSharp-Editor-firstpass";
        }
    }
}