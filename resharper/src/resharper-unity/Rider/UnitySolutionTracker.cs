using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnitySolutionTracker
    {
        private readonly ISolution mySolution;
        public readonly RProperty<bool> IsUnityProjectFolder = new RProperty<bool>();
        public readonly RProperty<bool> IsUnityGeneratedProject = new RProperty<bool>();
        public readonly RProperty<bool> IsUnityProject = new RProperty<bool>();

        public UnitySolutionTracker(ISolution solution, IFileSystemTracker fileSystemTracker, Lifetime lifetime, IShellLocks locks,
            UnityHost unityHost, UnityReferencesTracker unityReferencesTracker)
        {
            mySolution = solution;
            if (locks.Dispatcher.IsAsyncBehaviorProhibited) // for tests
                return;

            SetValues();

            fileSystemTracker.AdviseDirectoryChanges(lifetime, mySolution.SolutionDirectory.Combine(ProjectExtensions.AssetsFolder), false,
                OnChangeAction);
            // track not only folder itself, but also files inside
            fileSystemTracker.AdviseDirectoryChanges(lifetime, mySolution.SolutionDirectory.Combine(ProjectExtensions.ProjectSettingsFolder), true,
                OnChangeActionProjectSettingsFolder);
            
            unityHost.PerformModelAction(model =>
            {
                unityReferencesTracker.HasUnityReference.Advise(lifetime, res => { model.HasUnityReference.SetValue(res); });
            });
        }

        private void SetValues()
        {
            IsUnityProjectFolder.SetValue(HasUnityFileStructure(mySolution.SolutionDirectory));
            IsUnityProject.SetValue(IsUnityProjectFolder.Value && mySolution.IsValid() &&
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