﻿using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Resources;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Occurrences
{
    // Group usages of the name element by "Assembly definition reference"
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class AsmDefOccurrenceKindProvider : IOccurrenceKindProvider
    {
        public static readonly OccurrenceKind AssemblyDefinitionReference =
            OccurrenceKind.CreateSemantic(Strings.AsmDefOccurrenceKindProvider_AssemblyDefinitionReference_Assembly_definition_reference);

        public ICollection<OccurrenceKind>? GetOccurrenceKinds(IOccurrence occurrence)
        {
            if (occurrence is AsmDefNameOccurrence)
                return new[] { AssemblyDefinitionReference };

            var referenceOccurrence = occurrence as ReferenceOccurrence;
            var reference = referenceOccurrence?.PrimaryReference;
            return reference is AsmDefNameReference ? new[] { AssemblyDefinitionReference } : null;
        }

        public IEnumerable<OccurrenceKind> GetAllPossibleOccurrenceKinds() => new[] { AssemblyDefinitionReference };
    }
}