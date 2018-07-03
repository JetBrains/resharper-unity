using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.Refactorings
{
    // Support rename of both the references to the string literal value of the "name" property, and the literal value
    // itself. The standard AtomicRename fails (with asserts), because our declared element doesn't have any declarations.
    // If we change AsmDefNameDeclaredElement to implement IRenameableDeclaredElement, then JavaScriptAtomicRenameBase
    // will rename the references, but will not find any declarations to rename. So implement our own AtomicRename that
    // handles everything - renaming references and replacing the original tree node string literal
    [ShellFeaturePart]
    public class AsmDefRenamesFactory : AtomicRenamesFactory
    {
        public override bool IsApplicable(IDeclaredElement declaredElement)
        {
            return declaredElement is AsmDefNameDeclaredElement;
        }

        public override RenameAvailabilityCheckResult CheckRenameAvailability(IDeclaredElement declaredElement)
        {
            if (declaredElement is AsmDefNameDeclaredElement)
                return RenameAvailabilityCheckResult.CanBeRenamed;
            return RenameAvailabilityCheckResult.CanNotBeRenamed;
        }

        public override IEnumerable<AtomicRenameBase> CreateAtomicRenames(IDeclaredElement declaredElement, string newName, bool doNotAddBindingConflicts)
        {
            if (declaredElement is AsmDefNameDeclaredElement asmDefNameDeclaredElement)
                yield return new AsmDefNameAtomicRename(asmDefNameDeclaredElement, newName);
        }
    }
}