using System.Linq;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class UnityCacheUpdater
    {
        public UnityCacheUpdater(Lifetime lifetime, ISolution solution, UnityYamlSupport unityYamlSupport, IShellLocks locks, ChangeManager changeManager, UnityExternalFilesModuleFactory factory)
        {
            var module = factory.PsiModule;
            if (module != null)
            {
                unityYamlSupport.IsUnityYamlParsingEnabled.Change.Advise_NoAcknowledgement(lifetime, (handler) =>
                {
                    if (handler.HasNew && handler.HasOld && handler.New == handler.Old)
                        return;

                    locks.ExecuteOrQueueReadLockEx(lifetime, "YamlParsingStateChange", () =>
                    {
                        var psiSourceFiles = module.SourceFiles.ToList();
                        if (psiSourceFiles.Any())
                        {
                            locks.ExecuteWithWriteLock(() => changeManager.ExecuteAfterChange(() =>
                            {
                                var changeBuilder = new PsiModuleChangeBuilder();
                                foreach (var sourceFile in psiSourceFiles)
                                {
                                    if (sourceFile.IsValid())
                                        changeBuilder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Modified);
                                }
                                changeManager.OnProviderChanged(solution, changeBuilder.Result, SimpleTaskExecutor.Instance);
                            }));
                        }
                    });
                });
            }
        }
    }
}