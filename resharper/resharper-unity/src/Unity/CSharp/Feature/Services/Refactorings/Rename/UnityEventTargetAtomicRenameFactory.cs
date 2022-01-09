using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
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

            switch (clrDeclaredElement)
            {
                case IProperty _:
                case IMethod _:
                    var containingType = clrDeclaredElement.GetContainingType();
                    if (containingType == null)
                        return false;

                    var solution = clrDeclaredElement.GetSolution();

                    var knownTypesCache = solution.GetComponent<KnownTypesCache>();
                    var unityObjectType = knownTypesCache.GetByClrTypeName(KnownTypes.Object, clrDeclaredElement.Module).GetTypeElement();
                    var result = containingType.IsDescendantOf(unityObjectType);
                    if (!result)
                        return false;
                    var cacheController = solution.GetComponent<DeferredCacheController>();

                    if (cacheController.IsProcessingFiles())
                        return true;

                    var methods = solution.GetComponent<UnityEventsElementContainer>();
                    return methods.GetAssetUsagesCount(clrDeclaredElement, out bool estimatedResult) > 0 || estimatedResult;

                default:
                    return false;
            }
        }
    }
}
