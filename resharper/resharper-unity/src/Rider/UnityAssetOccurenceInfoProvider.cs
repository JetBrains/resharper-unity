using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Occurrences.OccurrenceInformation;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
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
            return OccurrenceMergeContext.Empty;
        }

        public TextRange GetTextRange(IOccurrence occurrence)
        {
            return (occurrence as UnityAssetOccurrence).TextRange;
        }

        public ProjectModelElementEnvoy GetProjectModelElementEnvoy(IOccurrence occurrence)
        {
            return null;
        }

        public bool IsApplicable(IOccurrence occurrence)
        {
            return occurrence is UnityAssetOccurrence;
        }

        public void SetTabOptions(TabOptions tabOptions, IOccurrence occurrence)
        {
        }
    }
}