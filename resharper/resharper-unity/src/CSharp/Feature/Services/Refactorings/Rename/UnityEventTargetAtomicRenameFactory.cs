using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    [ShellFeaturePart]
    public class UnityEventTargetAtomicRenameFactory : IAtomicRenameFactory
    {
        public bool IsApplicable(IDeclaredElement declaredElement)
        {
            if (!declaredElement.IsFromUnityProject())
                return false;

            return IsPossibleEventHandler(declaredElement);
        }

        public RenameAvailabilityCheckResult CheckRenameAvailability(IDeclaredElement element)
        {
            return RenameAvailabilityCheckResult.CanBeRenamed;
        }

        public IEnumerable<AtomicRenameBase> CreateAtomicRenames(IDeclaredElement declaredElement, string newName,
                                                                 bool doNotAddBindingConflicts)
        {
            return new[] {new UnityEventTargetAtomicRename(declaredElement.GetSolution(), declaredElement, newName)};
        }

        private static bool IsPossibleEventHandler(IDeclaredElement declaredElement)
        {
            var clrDeclaredElement = declaredElement as IClrDeclaredElement;

            var containingType = clrDeclaredElement?.GetContainingType();
            if (containingType == null)
                return false;
            
            var unityObjectType = TypeFactory.CreateTypeByCLRName(KnownTypes.Object, clrDeclaredElement.Module).GetTypeElement();
            return containingType.IsDescendantOf(unityObjectType);
        }
    }
}
