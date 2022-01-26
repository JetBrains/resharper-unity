using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.DocumentManagers;
using JetBrains.DocumentManagers.Transactions;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.Impl;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Documents
{
    /// <summary>
    /// Removes auto sync through disk for shared files, to avoid unity refresh
    /// </summary>
    [SolutionComponent]
    public class RiderUnitySharedFilesSavingSuppressor : IRiderDocumentSavingSuppressor
    {
        [NotNull] private readonly ISolution mySolution;
        [NotNull] private readonly UnitySolutionTracker myUnitySolutionTracker;
        [NotNull] private readonly DocumentToProjectFileMappingStorage myDocumentToProjectFileMappingStorage;
        [NotNull] private readonly ILogger myLogger;

        public RiderUnitySharedFilesSavingSuppressor(
            [NotNull] ISolution solution,
            [NotNull] UnitySolutionTracker unitySolutionTracker,
            [NotNull] DocumentToProjectFileMappingStorage documentToProjectFileMappingStorage,
            [NotNull] ILogger logger)
        {
            mySolution = solution;
            myUnitySolutionTracker = unitySolutionTracker;
            myDocumentToProjectFileMappingStorage = documentToProjectFileMappingStorage;
            myLogger = logger;
        }

        public bool ShouldSuppress(IDocument document, bool forceSaveOpenDocuments)
        {
            if (!forceSaveOpenDocuments) return false;

            var projectFile = myDocumentToProjectFileMappingStorage.TryGetProjectFile(document);
            var isUnitySharedProjectFile = projectFile != null
                                           && myUnitySolutionTracker.IsUnityGeneratedProject.Value
                                           && projectFile.IsShared();

            if (isUnitySharedProjectFile)
            {
                if (!IsFileAssociatedWithOpenedEditor(document)) return true;

                mySolution.Locks.ExecuteOrQueueWithWriteLockWhenAvailableEx(Lifetime.Eternal, "Sync Unity shared files", () =>
                {
                    using (mySolution.CreateTransactionCookie(DefaultAction.Commit, "Sync Unity shared files"))
                    {
                        var text = projectFile.GetDocument().GetText();
                        foreach (var sharedProjectFile in projectFile.GetSharedProjectFiles())
                        {
                            if (sharedProjectFile == projectFile) continue;
                            sharedProjectFile.GetDocument().SetText(text);
                        }
                    }
                });

                myLogger.Verbose("File is shared and contained in Unity project. Skip saving.");
                return true;
            }

            return false;
        }

        private bool IsFileAssociatedWithOpenedEditor(IDocument document)
        {
            var modifiedProjectFile = myDocumentToProjectFileMappingStorage.TryGetProjectFile(document);
            if (modifiedProjectFile == null)
                return false;

            var documentModel = modifiedProjectFile.GetData(DocumentHostBase.DocumentModelKey);
            return documentModel != null && !documentModel.TextControls.IsEmpty();
        }
    }
}