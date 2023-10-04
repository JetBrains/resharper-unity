#nullable enable

using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Impl.Shared.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
    [ReferenceProviderFactory]
    public class UxmlReferenceProviderFactory : IReferenceProviderFactory
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public UxmlReferenceProviderFactory(Lifetime lifetime, UnitySolutionTracker unitySolutionTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;

            Changed = new DataFlow.Signal<IReferenceProviderFactory>(GetType().FullName!);
        }

        public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex? wordIndexForChecks)
        {
            if (!sourceFile.IsValid())
                return null;

            // The source file might be in the external files module, so we can't look at what project it belongs to
            if (!myUnitySolutionTracker.IsUnityProject.HasTrueValue())
                return null;

            if (sourceFile.IsUxml() && sourceFile.IsLanguageSupported<UxmlLanguage>() )
                return new UxmlReferenceFactory();

            return null;
        }

        public DataFlow.ISignal<IReferenceProviderFactory> Changed { get; }
    }
    
    internal class UxmlReferenceFactory : IReferenceFactory
    {
        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            var compositeElement = element as CompositeElementWithReferences;
            if (compositeElement == null) return ReferenceCollection.Empty;

            
            var customReferences = compositeElement.CreateCustomReferences();
            if (customReferences.Count == 0) return ReferenceCollection.Empty;

            if (customReferences.Count == oldReferences.Count)
            {
                for (var index = 0; index < oldReferences.Count; index++)
                {
                    var oldReference = oldReferences[index];
                    var newReference = customReferences[index];

                    if (!oldReference.IsValid()
                        || oldReference.GetType() != newReference.GetType()
                        || oldReference.GetTreeNode() != newReference.GetTreeNode()
                        || oldReference.GetTreeTextRange() != newReference.GetTreeTextRange())
                    {
                        return customReferences;
                    }
                }

                return oldReferences;
            }

            return customReferences;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            if (element is not CompositeElementWithReferences compositeElement) return false;

            return compositeElement.GetReferences().Count > 0;
        }
    }
}