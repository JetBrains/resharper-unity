using System.Linq;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class UnityCacheUpdater
    {
        public UnityCacheUpdater(Lifetime lifetime, ISolution solution, UnityYamlEnabled unityYamlEnabled, IShellLocks locks, ChangeManager changeManager, UnityExternalFilesModuleFactory factory)
        {
            var module = factory.PsiModule;
            if (module != null)
            {
                unityYamlEnabled.YamlParsingEnabled.Change.Advise_NoAcknowledgement(lifetime, (handler) =>
                {
                    if (!handler.HasNew  || !handler.HasNew)
                        return;
                    locks.ExecuteOrQueueReadLockEx(lifetime, "YamlParsingEnabled", () =>

                    {
                        var psiSourceFiles = module.SourceFiles.ToList();
                        if (psiSourceFiles.Any())
                        {
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
                        }
                    });
                });
            }
        }
    }
}