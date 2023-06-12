using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.Refactorings.Rename
{
    [ShellFeaturePart]
    public class ShaderLabRenamesFactory : AtomicRenamesFactory
    {
        public override bool IsApplicable(IDeclaredElement declaredElement) =>
            declaredElement.GetElementType() is var elementType && (elementType == ShaderLabDeclaredElementType.Shader || elementType == ShaderLabDeclaredElementType.TexturePass);
        
        public override RenameAvailabilityCheckResult CheckRenameAvailability(IDeclaredElement declaredElement)
        {
            Assertion.Assert(IsApplicable(declaredElement), "CheckRenameAvailability for element where renaming isn't applicable.");
            foreach (var sourceFile in declaredElement.GetSourceFiles())
            {
                if (sourceFile.Properties.IsNonUserFile)
                    return RenameAvailabilityCheckResult.IsCompiledElement;
            }
            return RenameAvailabilityCheckResult.CanBeRenamed;
        }

        public override IEnumerable<AtomicRenameBase> CreateAtomicRenames(IDeclaredElement declaredElement, string newName, bool doNotAddBindingConflicts)
        {
            Assertion.Assert(IsApplicable(declaredElement), "CreateAtomicRenames for element where renaming isn't applicable.");
            yield return new ShaderLabAtomicRename(declaredElement, newName);
        }
    }
}