using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Transaction;
using JetBrains.RdBackend.Common.Env;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Psi.Modules
{
    // Rider requires us to have an IProjectFile for the IPsiSourceFiles in the external files module. This is primarily
    // because PSI modifications won't be saved without one. When a PSI transaction completes, Rider looks for a project
    // file for the modified document. If it finds one, it binds the document to the protocol and all changes are passed
    // to the frontend. When the transaction saves, it only saves on the frontend, and because the frontend document has
    // been modified, the changes are saved. Without a project file, the changes are not sent to the frontend, and the
    // frontend document is not modified (and possibly doesn't exist) and therefore nothing is saved.
    // Arguably, the platform should still bind documents that don't have project files.
    // We do not modify .meta files, so we don't need a project file for them (there's too many of them anyway. Perhaps
    // the platform could allow us to create on demand?)
    // ReSharper does not require project files for saving documents (but has its own workarounds for similar issues).
    // Creating project files in the Misc Files project is not allowed - this project needs to be kept in sync with VS.
    [SolutionComponent]
    public class RiderMiscFilesProjectFileManager : IChangeProvider
    {
        private readonly ISolution mySolution;

        public RiderMiscFilesProjectFileManager(Lifetime lifetime,
                                                ISolution solution,
                                                ChangeManager changeManager,
                                                UnityExternalFilesModuleProcessor externalFilesProcessor)
        {
            mySolution = solution;

            changeManager.RegisterChangeProvider(lifetime, this);
            changeManager.AddDependency(lifetime, this, externalFilesProcessor);
        }

        public object Execute(IChangeMap changeMap)
        {
            using (new ProjectModelBatchChangeCookie(mySolution, SimpleTaskExecutor.Instance))
            using (mySolution.Locks.UsingWriteLock())
            {
                foreach (var psiModuleChange in changeMap.GetChanges<PsiModuleChange>())
                {
                    foreach (var fileChange in psiModuleChange.FileChanges)
                    {
                        if (fileChange.Item is not UnityExternalPsiSourceFile file)
                            continue;

                        switch (fileChange.Type)
                        {
                            case PsiModuleChange.ChangeType.Added:
                                // We don't need a project file for meta files
                                if (!file.Location.IsMeta())
                                    AddExternalProjectFiles(file.Location);
                                break;

                            case PsiModuleChange.ChangeType.Removed:
                                RemoveExternalProjectFiles(file.Location);
                                break;

                            case PsiModuleChange.ChangeType.Modified:
                            case PsiModuleChange.ChangeType.Invalidated:
                                break;
                        }
                    }
                }
            }

            return null;
        }

        private void AddExternalProjectFiles(VirtualFileSystemPath path)
        {
            if (mySolution.FindProjectItemsByLocation(path).Count > 0)
                return;

            // Avoid SolutionMiscFiles.CreateMiscFile as this will involve transactions, and loading/saving the document
            // for each file
            DocumentHostBase.CreateMiscFile(mySolution, path);
        }

        private void RemoveExternalProjectFiles(VirtualFileSystemPath path)
        {
            var toRemove = new FrugalLocalList<ProjectItemBase>();
            foreach (var projectItem in mySolution.FindProjectItemsByLocation(path))
            {
                if (projectItem is ProjectItemBase projectFile && projectFile.IsMiscProjectItem())
                    toRemove.Add(projectFile);
            }

            foreach (var projectItemBase in toRemove)
                projectItemBase.DoRemove();
        }
    }
}
