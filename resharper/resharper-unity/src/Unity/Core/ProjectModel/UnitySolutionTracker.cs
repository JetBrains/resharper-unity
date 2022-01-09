using System;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    [SolutionComponent]
    public class UnitySolutionTracker
    {
        private readonly ISolution mySolution;

        public readonly ViewableProperty<bool> IsUnityProjectFolder = new ViewableProperty<bool>();
        public readonly ViewableProperty<bool> IsUnityGeneratedProject = new ViewableProperty<bool>();
        public readonly ViewableProperty<bool> IsUnityProject = new ViewableProperty<bool>();

        public UnitySolutionTracker(ISolution solution, IFileSystemTracker fileSystemTracker, Lifetime lifetime, bool inTests = false)
        {
            mySolution = solution;
            if (inTests)
            {
                IsUnityGeneratedProject.Value = false;
                IsUnityProject.Value = false;
                IsUnityProjectFolder.Value = false;
                return;
            }

            SetValues();

            fileSystemTracker.AdviseDirectoryChanges(lifetime, mySolution.SolutionDirectory.Combine(ProjectExtensions.AssetsFolder), false,
                OnChangeAction);
            // track not only folder itself, but also files inside
            fileSystemTracker.AdviseDirectoryChanges(lifetime, mySolution.SolutionDirectory.Combine(ProjectExtensions.ProjectSettingsFolder), true,
                OnChangeActionProjectSettingsFolder);
        }

        private void SetValues()
        {
            IsUnityProjectFolder.SetValue(HasUnityFileStructure(mySolution.SolutionDirectory));
            IsUnityProject.SetValue(IsUnityProjectFolder.Value && mySolution.IsValid() && mySolution.SolutionFilePath.ExistsFile);
            IsUnityGeneratedProject.SetValue(IsUnityProject.Value && SolutionNameMatchesUnityProjectName());
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
            return string.Equals(path.Name, "ProjectSettings", StringComparison.OrdinalIgnoreCase)
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
            return string.Equals(mySolution.SolutionFilePath.NameWithoutExtension, mySolution.SolutionDirectory.Name,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasUnityFileStructure(VirtualFileSystemPath solutionDir)
        {
            var assetsFolder = solutionDir.CombineWithShortName(ProjectExtensions.AssetsFolder);
            var projectSettingsFolder = solutionDir.CombineWithShortName(ProjectExtensions.ProjectSettingsFolder);
            var projectVersionTxtFile = projectSettingsFolder.CombineWithShortName("ProjectVersion.txt");
            return assetsFolder.IsAbsolute && assetsFolder.ExistsDirectory
                                           && projectSettingsFolder.IsAbsolute && projectSettingsFolder.ExistsDirectory
                                           && (projectVersionTxtFile.IsAbsolute && projectVersionTxtFile.ExistsFile
                                               || projectSettingsFolder.GetChildFiles("*.asset").Any());
        }
    }
}