using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Yaml
{
    [SolutionComponent]
    public class TestUnityExternalFilesModuleProcessor : UnityExternalFilesModuleProcessor
    {
        public TestUnityExternalFilesModuleProcessor(Lifetime lifetime, ILogger logger, ISolution solution, ChangeManager changeManager, IShellLocks locks, ISolutionLoadTasksScheduler scheduler, IFileSystemTracker fileSystemTracker, ProjectFilePropertiesFactory projectFilePropertiesFactory, UnityYamlPsiSourceFileFactory psiSourceFileFactory, UnityExternalFilesModuleFactory moduleFactory, UnityYamlDisableStrategy unityYamlDisableStrategy)
            : base(lifetime, logger, solution, changeManager, locks, scheduler, fileSystemTracker, projectFilePropertiesFactory, psiSourceFileFactory, moduleFactory, unityYamlDisableStrategy)
        {
        }

        public override void OnUnityProjectAdded(Lifetime projectLifetime, IProject project)
        {
        }
    }
}