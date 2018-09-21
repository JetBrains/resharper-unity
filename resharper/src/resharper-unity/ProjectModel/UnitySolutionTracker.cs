using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    [SolutionComponent]
    public class UnitySolutionTracker
    {
        private readonly ISolution mySolution;
        public readonly RProperty<bool> IsAbleToEstablishProtocolConnectionWithUnity;

        public UnitySolutionTracker(ISolution solution, IFileSystemTracker fileSystemTracker, Lifetime lifetime, IShellLocks locks)
        {
            mySolution = solution;
            
            IsAbleToEstablishProtocolConnectionWithUnity = new RProperty<bool>();
            if (locks.Dispatcher.IsAsyncBehaviorProhibited) // for tests
                return;

            IsAbleToEstablishProtocolConnectionWithUnity.SetValue(
                ProjectExtensions.IsAbleToEstablishProtocolConnectionWithUnity(mySolution.SolutionDirectory));
            
            fileSystemTracker.AdviseDirectoryChanges(lifetime, mySolution.SolutionDirectory.Combine(ProjectExtensions.AssetsFolder), false,
                OnChangeAction);
            fileSystemTracker.AdviseDirectoryChanges(lifetime, mySolution.SolutionDirectory.Combine(ProjectExtensions.LibraryFolder), false,
                OnChangeAction);
            fileSystemTracker.AdviseDirectoryChanges(lifetime, mySolution.SolutionDirectory.Combine(ProjectExtensions.ProjectSettingsFolder), false,
                OnChangeAction);
        }

        private void OnChangeAction (FileSystemChangeDelta delta)
        {
            if (delta.ChangeType == FileSystemChangeType.ADDED || delta.ChangeType == FileSystemChangeType.DELETED)
            {
                IsAbleToEstablishProtocolConnectionWithUnity.SetValue(
                    ProjectExtensions.IsAbleToEstablishProtocolConnectionWithUnity(mySolution.SolutionDirectory));
            }
        }
    }
}