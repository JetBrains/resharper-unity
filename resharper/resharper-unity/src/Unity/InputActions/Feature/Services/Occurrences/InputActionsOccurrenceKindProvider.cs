using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Resolve;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Feature.Services.Occurrences
{
    // Group usages of the name element by "Assembly definition reference"
    [SolutionComponent]
    public class InputActionsOccurrenceKindProvider : IOccurrenceKindProvider
    {
        public static readonly OccurrenceKind InputActionsReference =
            OccurrenceKind.CreateSemantic("Input actions reference");

        public ICollection<OccurrenceKind>? GetOccurrenceKinds(IOccurrence occurrence)
        {
            if (occurrence is InputActionsNameOccurrence)
                return new[] { InputActionsReference };

            var referenceOccurrence = occurrence as ReferenceOccurrence;
            var reference = referenceOccurrence?.PrimaryReference;
            return reference is UnityInputActionsReference ? new[] { InputActionsReference } : null;
        }

        public IEnumerable<OccurrenceKind> GetAllPossibleOccurrenceKinds() => new[] { InputActionsReference };
    }
}