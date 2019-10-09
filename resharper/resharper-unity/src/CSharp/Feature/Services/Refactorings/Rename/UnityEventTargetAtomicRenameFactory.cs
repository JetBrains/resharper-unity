using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
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

            return IsEventHandler(declaredElement);
        }

        public RenameAvailabilityCheckResult CheckRenameAvailability(IDeclaredElement element)
        {
            return RenameAvailabilityCheckResult.CanBeRenamed;
        }

        public IEnumerable<AtomicRenameBase> CreateAtomicRenames(IDeclaredElement declaredElement, string newName,
                                                                 bool doNotAddBindingConflicts)
        {
            return new[] {new UnityEventTargetAtomicRename(declaredElement, newName)};
        }

        private static bool IsEventHandler(IDeclaredElement declaredElement)
        {
            var eventHandlerCache = declaredElement.GetSolution().GetComponent<UnitySceneDataLocalCache>();
            switch (declaredElement)
            {
                case IMethod method:
                    return eventHandlerCache.IsEventHandler(method);

                case IProperty property:
                    var setter = property.Setter;
                    return setter != null && eventHandlerCache.IsEventHandler(setter);

                default:
                    return false;
            }
        }
    }
}
