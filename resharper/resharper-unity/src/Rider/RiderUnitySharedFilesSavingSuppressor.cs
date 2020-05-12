using JetBrains.Annotations;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.Documents;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    /// <summary>
    /// Removes auto sync through disk for shared files, to avoid unity refresh
    /// </summary>
    [SolutionComponent]
    public class RiderUnitySharedFilesSavingSuppressor : IRiderDocumentSavingSuppressor
    {
        [NotNull] private readonly UnitySolutionTracker myUnitySolutionTracker;
        [NotNull] private readonly DocumentToProjectFileMappingStorage myDocumentToProjectFileMappingStorage;
        [NotNull] private readonly ILogger myLogger;

        public RiderUnitySharedFilesSavingSuppressor(
            [NotNull] UnitySolutionTracker unitySolutionTracker,
            [NotNull] DocumentToProjectFileMappingStorage documentToProjectFileMappingStorage,
            [NotNull] ILogger logger)
        {
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
                myLogger.Verbose("File is shared and contained in Unity project. Skip saving.");
                return true;
            }

            return false;
        }
    }
}