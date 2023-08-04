using System;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel
{
    [SolutionComponent]
    public class UnitySolutionTracker : IUnityReferenceChangeHandler
    {
        private readonly ISolution mySolution;
        private readonly VirtualFileSystemPath mySolutionDirectory;

        public readonly ViewableProperty<bool> IsUnityProjectFolder = new();
        public readonly ViewableProperty<bool> IsUnityProject = new();
        [Obsolete("Use IsUnityProject instead")] // Only use this for collecting statistics
        public readonly ViewableProperty<bool> IsUnityGeneratedProject = new();

        // If all you're interested in is being notified that we're a Unity solution, advise this. If you need to know
        // we're a Unity solution *and*/or know about Unity projects (and get a per-project lifetime), implement
        // IUnityReferenceChangeHandler
        public readonly ViewableProperty<bool> HasUnityReference = new(false);

        public UnitySolutionTracker(ISolution solution, IFileSystemTracker fileSystemTracker, Lifetime lifetime,
                                    bool inTests = false)
        {
            mySolution = solution;
            if (inTests)
            {
                IsUnityProject.Value = false;
                IsUnityProjectFolder.Value = false;
                HasUnityReference.Value = false;
                return;
            }

            // SolutionDirectory isn't absolute in tests, and will throw an exception if we use it when we call Exists
            mySolutionDirectory = solution.SolutionDirectory;
            if (!mySolutionDirectory.IsAbsolute)
                mySolutionDirectory = solution.SolutionDirectory.ToAbsolutePath(FileSystemUtil.GetCurrentDirectory().ToVirtualFileSystemPath());

            SetValues();

            fileSystemTracker.AdviseDirectoryChanges(lifetime,
                mySolutionDirectory.Combine(ProjectExtensions.AssetsFolder), false, OnChangeAction);
            // track not only folder itself, but also files inside
            fileSystemTracker.AdviseDirectoryChanges(lifetime,
                mySolutionDirectory.Combine(ProjectExtensions.ProjectSettingsFolder), true,
                OnChangeActionProjectSettingsFolder);
        }

        private void SetValues()
        {
            // Note that HasLibraryFolder is not part of HasUnityFileStructure because it's not pushed to VCS. If we try
            // to open a clean checkout of a Unity solution (perhaps just as a directory, not a solution), then
            // IsUnityProjectFolder will be true, but everything else will be false. Once we have a solution file, that
            // means Unity has opened the project, generated a solution file and we'll also have a Library folder
            IsUnityProjectFolder.SetValue(HasUnityFileStructure(mySolutionDirectory));
            IsUnityProject.SetValue(IsUnityProjectFolder.Value && mySolution.IsValid() &&
                                    mySolution.SolutionFilePath.ExistsFile &&
                                    HasLibraryFolder(mySolutionDirectory));
#pragma warning disable CS0618 // Type or member is obsolete
            IsUnityGeneratedProject.SetValue(IsUnityProject.Value && SolutionNameMatchesUnityProjectName());
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private void OnChangeAction(FileSystemChangeDelta delta)
        {
            if (delta.ChangeType == FileSystemChangeType.ADDED || delta.ChangeType == FileSystemChangeType.DELETED)
                SetValues();
        }

        private void OnChangeActionProjectSettingsFolder(FileSystemChangeDelta delta)
        {
            if (IsInterestingProjectSettingsFile(delta.NewPath) || IsInterestingProjectSettingsFile(delta.OldPath))
                OnChangeAction(delta);
        }

        private static bool IsInterestingProjectSettingsFile(IPath path)
        {
            // Don't rely on correct casing. We expect Unity to generate the names correctly, but they could be edited
            // by other tooling that doesn't respect case. We've seen similar problems with solution names (see below)
            return string.Equals(path.Name, ProjectExtensions.ProjectSettingsFolder, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(path.Name, "ProjectVersion.txt", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(path.ExtensionNoDot, "asset", StringComparison.OrdinalIgnoreCase);
        }

        private bool SolutionNameMatchesUnityProjectName()
        {
            // https://github.com/JetBrains/resharper-unity/issues/2027
            // It is very unusual for the solution file to not match the case of the folder it's generated in, but it is
            // a potential situation. Perhaps the user has edited/renamed the solution file manually, or the folder, or
            // some other tooling has done something. If we don't match case, we'll break meta file handling, which is
            // very bad.
            // Furthermore, loading a solution from Rider's recent solution list keeps the old case, so even if things
            // have been renamed to match, we'll still have incorrect casing here. So let's just do case insensitive.
            return string.Equals(mySolution.SolutionFilePath.NameWithoutExtension, mySolutionDirectory.Name,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasUnityFileStructure(VirtualFileSystemPath solutionDir)
        {
            var assetsFolder = solutionDir.CombineWithShortName(ProjectExtensions.AssetsFolder);
            var projectSettingsFolder = solutionDir.CombineWithShortName(ProjectExtensions.ProjectSettingsFolder);
            var projectVersionTxtFile = projectSettingsFolder.CombineWithShortName("ProjectVersion.txt");
            return assetsFolder.IsAbsolute && assetsFolder.ExistsDirectory
                && projectSettingsFolder.IsAbsolute && projectSettingsFolder.ExistsDirectory
                && (projectVersionTxtFile.IsAbsolute && projectVersionTxtFile.ExistsFile || projectSettingsFolder.GetChildFiles("*.asset").Any());
        }

        private static bool HasLibraryFolder(VirtualFileSystemPath solutionDir)
        {
            var folder = solutionDir.CombineWithShortName(ProjectExtensions.LibraryFolder);
            return folder.IsAbsolute && folder.ExistsDirectory;
        }

        public void OnHasUnityReference()
        {
            // called once
            HasUnityReference.Set(true);
        }

        public void OnUnityProjectAdded(Lifetime projectLifetime, IProject project)
        {
            // do nothing
        }
    }
}