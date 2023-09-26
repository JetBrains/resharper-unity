#nullable enable

using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;

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

            if (sourceFile.IsUxml() && sourceFile.IsLanguageSupported<XmlLanguage>() )
                return new UxmlReferenceFactory();

            return null;
        }

        public DataFlow.ISignal<IReferenceProviderFactory> Changed { get; }
    }
}