#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.Tasks.Listeners;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Runtime
{
    // If the solution can't be loaded (e.g. we don't have a suitable MSBuild yet, see UnityBundledSdkRefresher),
    // no Unity project is added and UnityVersion.OnUnityProjectAdded is not called, so we never learn the editor
    // location. Only in that case, i.e. when the solution finished loading without a Unity project, read it from disk
    [SolutionComponent(Instantiation.ContainerAsyncAnyThreadSafe)]
    public class UnityAppPathFromProjectFilesFallback(
        ISolution solution,
        UnitySolutionTracker unitySolutionTracker,
        UnityVersion unityVersion,
        ILogger logger)
        : ISolutionLoadTasksAfterDoneListener2
    {
        IEnumerable<SolutionLoadTasksListenerExecutionStep> ISolutionLoadTasksAfterDoneListener2.OnSolutionLoadAfterDone(OuterLifetime loadLifetime)
        {
            if (solution.IsVirtualSolution())
                return EmptyList<SolutionLoadTasksListenerExecutionStep>.Enumerable;

            if (!unitySolutionTracker.IsUnityProject.HasTrueValue())
                return EmptyList<SolutionLoadTasksListenerExecutionStep>.Enumerable;

            // HasUnityReference is set from UnityReferencesTracker at PreparePsiModules, i.e. before we get
            // here, and only if at least one Unity project was actually added. If it's true, the project model
            // has given UnityVersion.OnUnityProjectAdded everything we could read from disk ourselves
            if (unitySolutionTracker.HasUnityReference.HasTrueValue())
                return EmptyList<SolutionLoadTasksListenerExecutionStep>.Enumerable;

            // if we know it, there is nothing to fall back to
            if (!unityVersion.ActualAppPathForSolution.Maybe.ValueOrDefault.IsNullOrEmpty())
                return EmptyList<SolutionLoadTasksListenerExecutionStep>.Enumerable;

            var appPath = FindAppPathInProjectFilesOnDisk(solution.SolutionDirectory);
            if (appPath == null)
            {
                logger.Info(
                    "No Unity project is loaded and the editor location isn't available in the project files on disk");
                return EmptyList<SolutionLoadTasksListenerExecutionStep>.Enumerable;
            }

            logger.Verbose($"No Unity project is loaded, the editor location is read from the project files on disk: {appPath}");
            unityVersion.SetAppPathFromProjectFilesOnDisk(appPath);
            
            return EmptyList<SolutionLoadTasksListenerExecutionStep>.Enumerable;
        }

        private VirtualFileSystemPath? FindAppPathInProjectFilesOnDisk(VirtualFileSystemPath solutionDirectory)
        {
            if (!solutionDirectory.IsAbsolute || !solutionDirectory.ExistsDirectory)
                return null;

            // Editor projects reference the assemblies of the editor installation itself, so they are the most likely
            // to give us the answer. Player projects also work, they just require more walking up the directories
            var projectFiles = solutionDirectory.GetChildFiles("*.csproj")
                .OrderByDescending(file => file.NameWithoutExtension.Contains("Editor"));

            return projectFiles.Select(GetAppPathByProjectFile).FirstOrDefault(appPath => !appPath.IsNullOrEmpty());
        }
        
        // Reads the editor location from a generated project file on disk. Normally we get this from the project model
        // (see UnityProjectFileCacheProvider, which calls GetAppPathByDll below for exactly the same data), so this
        // should only be used when the project model has nothing for us, i.e. when the solution can't be loaded
        // (see UnityAppPathFromProjectFilesFallback)
        private VirtualFileSystemPath? GetAppPathByProjectFile(VirtualFileSystemPath projectFile)
        {
            if (!projectFile.IsAbsolute || !projectFile.ExistsFile)
                return null;

            try
            {
                var document = new XmlDocument();
                document.LoadXml(projectFile.ReadAllText2().Text);

                var documentElement = document.DocumentElement;
                if (documentElement is not { Name: "Project" })
                    return null;

                return UnityInstallationFinder.GetAppPathByDll(documentElement);
            }
            catch (Exception e)
            {
                logger.Error(e, $"Unable to read the Unity editor location from {projectFile.Name}");
                return null;
            }
        }
    }
}
