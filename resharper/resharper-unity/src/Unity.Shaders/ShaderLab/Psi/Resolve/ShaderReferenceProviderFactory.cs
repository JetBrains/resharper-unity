#nullable enable

using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve
{
    // Creates references from the "references" array to the "name" declaration
    [ReferenceProviderFactory(Instantiation.DemandAnyThreadSafe)]
    public class ShaderReferenceProviderFactory : IReferenceProviderFactory
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public ShaderReferenceProviderFactory(Lifetime lifetime, UnitySolutionTracker unitySolutionTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;

            Changed = new DataFlow.Signal<IReferenceProviderFactory>(GetType().FullName!);
        }

        public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex? wordIndexForChecks)
        {
            // Make sure the source file is still valid. It's not clear why it would be invalid, but we get it
            // See DEXP-672670
            if (!sourceFile.IsValid())
                return null;

            // The source file might be in the external files module, so we can't look at what project it belongs to
            if (!sourceFile.GetSolution().HasUnityReference() && !myUnitySolutionTracker.IsUnityProject.HasTrueValue())
                return null;

            return sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>() ? new ShaderReferenceFactory() : null;
        }

        public DataFlow.ISignal<IReferenceProviderFactory> Changed { get; }
    }
}
