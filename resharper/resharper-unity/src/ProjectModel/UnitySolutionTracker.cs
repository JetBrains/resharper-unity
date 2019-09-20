using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    [SolutionInstanceComponent]
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
            IsUnityProject.SetValue(IsUnityProjectFolder.Value &&
                                    mySolution.SolutionFilePath.ExtensionNoDot.ToLower() == "sln");
            IsUnityGeneratedProject.SetValue(IsUnityProject.Value && SolutionNameMatchesUnityProjectName());
        }

        private void OnChangeAction(FileSystemChangeDelta delta)
        {
            if (delta.ChangeType == FileSystemChangeType.ADDED || delta.ChangeType == FileSystemChangeType.DELETED)
            {
                SetValues();
            }
        }

        private void OnChangeActionProjectSettingsFolder (FileSystemChangeDelta delta)
        {
            if (delta.NewPath.Name == "ProjectSettings" || delta.NewPath.Name == "ProjectVersion.txt" || delta.NewPath.ExtensionNoDot=="asset"
                ||
                delta.OldPath.Name == "ProjectSettings" || delta.OldPath.Name == "ProjectVersion.txt" || delta.OldPath.ExtensionNoDot=="asset")
            {
                OnChangeAction(delta);
            }
        }

        private bool SolutionNameMatchesUnityProjectName()
        {
            return mySolution.SolutionFilePath.NameWithoutExtension == mySolution.SolutionDirectory.Name;
        }

        private static bool HasUnityFileStructure(FileSystemPath solutionDir)
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