using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve
{
    // Creates references from the "references" array to the "name" declaration
    [ReferenceProviderFactory]
    public class AsmDefNameReferenceProviderFactory : IReferenceProviderFactory
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public AsmDefNameReferenceProviderFactory(Lifetime lifetime, UnitySolutionTracker unitySolutionTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;

            // ReSharper disable once AssignNullToNotNullAttribute
            Changed = new DataFlow.Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);
        }

        public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
        {
            // The source file might be in the external files module, so we can't look at what project it belongs to
            if (!sourceFile.GetSolution().HasUnityReference() && !myUnitySolutionTracker.IsUnityProject.HasTrueValue())
                return null;

            if (sourceFile.IsAsmDef() && sourceFile.PrimaryPsiLanguage.Is<JsonNewLanguage>())
                return new AsmDefNameReferenceFactory();

            return null;
        }

        public DataFlow.ISignal<IReferenceProviderFactory> Changed { get; }
    }
}