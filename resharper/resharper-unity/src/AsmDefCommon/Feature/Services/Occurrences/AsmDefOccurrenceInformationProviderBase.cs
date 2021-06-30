using JetBrains.Diagnostics;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Occurrences.OccurrenceInformation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Feature.Services.Occurrences
{
    public class AsmDefOccurrenceInformationProviderBase<T> : IOccurrenceInformationProvider2 where T : PsiLanguageType
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
            var asmDefNameOccurrence = (occurrence as AsmDefNameOccurrenceBase<T>).NotNull("asmDefNameOccurrence != null");
            return new TextRange(asmDefNameOccurrence.NavigationTreeOffset,
                asmDefNameOccurrence.NavigationTreeOffset + asmDefNameOccurrence.Name.Length);
        }

        public ProjectModelElementEnvoy GetProjectModelElementEnvoy(IOccurrence occurrence)
        {
            return null;
        }

        public SourceFilePtr GetSourceFilePtr(IOccurrence occurrence) =>
            (occurrence as AsmDefNameOccurrenceBase<T>)?.SourceFile.Ptr() ?? SourceFilePtr.Fake;

        public bool IsApplicable(IOccurrence occurrence) => occurrence is AsmDefNameOccurrenceBase<T>;

        public void SetTabOptions(TabOptions tabOptions, IOccurrence occurrence)
        {
        }
    }
}