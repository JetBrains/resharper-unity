using System;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel
{
    public static class ProjectExtensions
    {
        public const string AssetsFolder = "Assets";
        public const string PackagesFolder = "Packages";
        public const string ProjectSettingsFolder = "ProjectSettings";
        public const string LibraryFolder = "Library";

        public static bool HasUnityReference(this ISolution solution)
        {
            var tracker = solution.TryGetComponent<UnitySolutionTracker>();
            return tracker is { HasUnityReference.Value: true };
        }

        /// <summary>
        ///  Checks that specific project unity reference or specific unity guid
        /// </summary>
        public static bool IsUnityProject(this IProject? project)
        {
            if (project == null || !project.IsValid())
                return false;

            // Quicker than calling GetComponent
            var referenceTracker = project.GetData(UnityReferencesTracker.UnityReferencesTrackerKey);
            return referenceTracker != null && referenceTracker.IsUnityProject(project);
        }

        public static bool IsPlayerProject(this IProject? project)
        {
            if (project == null || !project.IsValid())
                return false;
            return IsPlayerProjectName(project.Name);
        }

        public static bool IsPlayerProjectName(string name) =>
            name.EndsWith(".Player", StringComparison.OrdinalIgnoreCase);

        public static string StripPlayerSuffix(string name) =>
            name.TrimFromEnd(".Player", StringComparison.OrdinalIgnoreCase);

        public static bool IsUnityGeneratedProject(this IProject? project)
        {
            if (project == null)
                return false;

            // This works for Assets for local Packages folders and for 'file:' based packages
            if (project.ProjectFileLocation.IsAbsolute)
            {
                return project.GetComponent<UnitySolutionTracker>().IsUnityProject.HasTrueValue();
            }
            // for our tests // todo: refactor tests so they also check logic above
            return project.HasSubItems(AssetsFolder) && IsUnityProject(project);
        }

        public static bool IsOneOfPredefinedUnityProjects(this IProject? project, bool includePlayerProjects = false) =>
            project != null && IsOneOfPredefinedUnityProjects(project.Name, includePlayerProjects);

        public static bool IsOneOfPredefinedUnityProjects(string projectName, bool includePlayerProjects = false)
        {
            // Editor projects obviously don't have player projects
            return IsAssemblyCSharp(projectName, includePlayerProjects)
                   || IsAssemblyCSharpEditor(projectName)
                   || IsAssemblyCSharpFirstpass(projectName, includePlayerProjects)
                   || IsAssemblyCSharpFirstpassEditor(projectName);
        }

        public static bool IsMainUnityProject(this IProject? project)
        {
            // TODO: The main project might be named anything if a user puts a .asmdef in the root of the Assets folder!
            return project != null && IsAssemblyCSharp(project.Name, false);
        }

        private static bool IsAssemblyCSharp(string projectName, bool includePlayerProject)
        {
            if (projectName.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase)) return true;
            return includePlayerProject && projectName.Equals("Assembly-CSharp.Player", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAssemblyCSharpFirstpass(string projectName, bool includePlayerProject)
        {
            if (projectName.Equals("Assembly-CSharp-firstpass", StringComparison.OrdinalIgnoreCase)) return true;
            return includePlayerProject && projectName.Equals("Assembly-CSharp-firstpass.Player", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAssemblyCSharpEditor(string projectName) =>
            projectName.Equals("Assembly-CSharp-Editor", StringComparison.OrdinalIgnoreCase);

        private static bool IsAssemblyCSharpFirstpassEditor(string projectName) =>
            projectName.Equals("Assembly-CSharp-Editor-firstpass", StringComparison.OrdinalIgnoreCase);

        public static IProject? GetMainUnityProject(this ISolution solution)
        {
            // TODO: If a .asmdef file is placed in the root of Assets, no Assembly-CSharp project will be generated!
            // If this returns null, find which project owns Assets/*.asmdef
            return solution.GetAssemblyCSharpProject();
        }

        /// <summary>
        /// Returns the Assembly-CSharp project. Can return null even in Unity projects!
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        private static IProject? GetAssemblyCSharpProject(this ISolution solution) =>
            solution.GetProjectByName("Assembly-CSharp");
    }
}