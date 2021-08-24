using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Feature.Services.Occurrences
{
    // Group usages of the name element by "Assembly definition reference"
    [SolutionComponent]
    public class AsmDefOccurrenceKindProvider : AsmDefOccurrenceKindProviderBase<AsmDefNameReference>
    {
        // TODO: Implementation from R# JSON based implementation
        // public static readonly OccurrenceKind AssemblyDefinitionReference = new OccurrenceKind("Assembly definition reference", OccurrenceKind.SemanticAxis);

        // public ICollection<OccurrenceKind> GetOccurrenceKinds(IOccurrence occurrence)
        // {
            // if (occurrence is AsmDefNameOccurrence)
                // return new[] {AssemblyDefinitionReference};

            // if (occurrence is JavaScriptReferenceOccurrence jsOccurrence
                // && jsOccurrence.SourceFile.IsAsmDef()
                // && jsOccurrence.PrimaryReference is AsmDefNameReference)
            // {
                // return new[] {AssemblyDefinitionReference};
            // }

            // return null;
        // }

        // public IEnumerable<OccurrenceKind> GetAllPossibleOccurrenceKinds()
        // {
            // return new[] {AssemblyDefinitionReference};
        // }
    }
}