using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve
{
    // Creates references from the "reference" property value to a "name" declaration
    [ReferenceProviderFactory]
    public class AsmRefNameReferenceProviderFactory : IReferenceProviderFactory
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public AsmRefNameReferenceProviderFactory(Lifetime lifetime, UnitySolutionTracker unitySolutionTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;

            Changed = new DataFlow.Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);
        }

        public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex? wordIndexForChecks)
        {
            // The source file might be in the external files module, so we can't look at what project it belongs to
            if (!sourceFile.GetSolution().HasUnityReference() && !myUnitySolutionTracker.IsUnityProject.HasTrueValue())
                return null;

            return sourceFile.IsAsmRef() && sourceFile.PrimaryPsiLanguage.Is<JsonNewLanguage>()
                ? new AsmRefNameReferenceFactory()
                : null;
        }

        public DataFlow.ISignal<IReferenceProviderFactory> Changed { get; }
    }
}