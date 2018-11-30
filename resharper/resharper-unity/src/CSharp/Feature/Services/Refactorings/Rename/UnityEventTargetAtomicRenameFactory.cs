using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    [ShellFeaturePart]
    public class UnityEventTargetAtomicRenameFactory : IAtomicRenameFactory
    {
        public bool IsApplicable(IDeclaredElement declaredElement)
        {
            if (declaredElement is IMethod method)
            {
                var eventHandlerCache = declaredElement.GetSolution().GetComponent<UnityEventHandlerReferenceCache>();
                return eventHandlerCache.IsEventHandler(method);
            }


            // TODO: Renaming properties

            return false;
        }

        // Disable rename completely for Unity event handlers
        public RenameAvailabilityCheckResult CheckRenameAvailability(IDeclaredElement element)
        {
            if (IsApplicable(element))
                return RenameAvailabilityCheckResult.CanNotBeRenamed;

            return RenameAvailabilityCheckResult.CanBeRenamed;
        }

        public IEnumerable<AtomicRenameBase> CreateAtomicRenames(IDeclaredElement declaredElement, string newName, bool doNotAddBindingConflicts)
        {
            return EmptyList<AtomicRenameBase>.Instance;
        }
    }
}