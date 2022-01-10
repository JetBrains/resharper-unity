using JetBrains.DocumentManagers;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Occurrences.OccurrenceInformation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    [SolutionFeaturePart]
    public class UnityAssetOccurenceInfoProvider : IOccurrenceInformationProvider2
    {
        public IDeclaredElementEnvoy GetTypeMember(IOccurrence occurrence)
        {
            return null;
        }

        public IDeclaredElementEnvoy GetTypeElement(IOccurrence occurrence)
        {
            return null;
        }

        public IDeclaredElementEnvoy GetNamespace(IOccurrence occurrence)
        {
            return null;
        }

        public OccurrenceMergeContext GetMergeContext(IOccurrence occurrence)
        {
            return new OccurrenceMergeContext(occurrence);
        }

        public TextRange GetTextRange(IOccurrence occurrence)
        {
            return TextRange.InvalidRange;
        }

        public ProjectModelElementEnvoy GetProjectModelElementEnvoy(IOccurrence occurrence)
        {
            // ReSharper's grouping is based on project files, so provide it if we've got it.
            // See RSCPP-21079
            var sourceFile = (occurrence as UnityAssetOccurrence)?.SourceFile;
            if (sourceFile != null)
            {
                var map = sourceFile.GetSolution().GetComponent<DocumentToProjectFileMappingStorage>();
                var miscFilesProjectFile = map.TryGetProjectFile(sourceFile.Document);
                return miscFilesProjectFile != null ? ProjectModelElementEnvoy.Create(miscFilesProjectFile) : null;
            }

            return null;
        }

        public SourceFilePtr GetSourceFilePtr(IOccurrence occurrence) =>
            (occurrence as UnityAssetOccurrence)?.SourceFile.Ptr() ?? SourceFilePtr.Fake;

        public bool IsApplicable(IOccurrence occurrence)
        {
            return occurrence is UnityAssetOccurrence;
        }

        public void SetTabOptions(TabOptions tabOptions, IOccurrence occurrence)
        {
        }
    }
}