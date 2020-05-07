using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Threading;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.Documents;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class RiderUnityDocumentOperationsImpl : RiderDocumentOperationsImpl
    {
        [NotNull] private readonly ILogger myLogger;


        public RiderUnityDocumentOperationsImpl(Lifetime lifetime,
            [NotNull] SolutionModel solutionModel,
            [NotNull] SettingsModel settingsModel,
            [NotNull] ISolution solution,
            [NotNull] IShellLocks locks,
            [NotNull] ChangeManager changeManager,
            [NotNull] DocumentToProjectFileMappingStorage documentToProjectFileMappingStorage,
            [NotNull] IFileSystemTracker fileSystemTracker,
            [NotNull] ILogger logger)
            : base(lifetime, solutionModel, settingsModel, solution, locks, changeManager,
                documentToProjectFileMappingStorage, fileSystemTracker, logger)
        {
            myLogger = logger;
        }

        public override void SaveDocumentAfterModification(IDocument document, bool forceSaveOpenDocuments)
        {
            Locks.Dispatcher.AssertAccess();

            if (forceSaveOpenDocuments)
            {
                var projectFile = DocumentToProjectFileMappingStorage.TryGetProjectFile(document);
                var isUnitySharedProjectFile = projectFile != null 
                                               && projectFile.IsShared() 
                                               && projectFile.GetProject().IsUnityProject();
                if (isUnitySharedProjectFile)
                {
                    myLogger.Info($"Trying to save document {document.Moniker}. Force = true");
                    myLogger.Verbose("File is shared and contained in Unity project. Skip saving.");
                    return;
                }
            }

            base.SaveDocumentAfterModification(document, forceSaveOpenDocuments);
        }
    }
}