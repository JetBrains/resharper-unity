using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Yaml
{
    [SolutionComponent]
    public class TestUnityExternalFilesModuleProcessor : UnityExternalFilesModuleProcessor
    {
        public TestUnityExternalFilesModuleProcessor(Lifetime lifetime, ILogger logger, ISolution solution, ChangeManager changeManager, IShellLocks locks, ISolutionLoadTasksScheduler scheduler, IFileSystemTracker fileSystemTracker, ProjectFilePropertiesFactory projectFilePropertiesFactory, UnityExternalPsiSourceFileFactory psiSourceFileFactory, UnityExternalFilesModuleFactory moduleFactory, UnityExternalFilesIndexDisablingStrategy indexDisablingStrategy)
            : base(lifetime, logger, solution, changeManager, locks, scheduler, fileSystemTracker, psiSourceFileFactory, moduleFactory, indexDisablingStrategy)
        {
        }

        public override void OnUnityProjectAdded(Lifetime projectLifetime, IProject project)
        {
        }
    }
}