using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Feature.Services.Occurrences
{
    // Group usages of the name element by "Assembly definition reference"
    [SolutionComponent]
    public class AsmDefOccurrenceKindProvider : AsmDefOccurrenceKindProviderBase<AsmDefNameReference>
    {
    }
}