using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.Resolve
{
    // Creates references from the "references" array to the "name" declaration
    [ReferenceProviderFactory]
    public class AsmDefNameReferenceProviderFactory : IReferenceProviderFactory
    {
        public AsmDefNameReferenceProviderFactory(Lifetime lifetime)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);
        }

        public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
        {
            var project = sourceFile.GetProject();
            if (project == null || !project.IsUnityProject())
                return null;

            if (sourceFile.IsAsmDef() && sourceFile.PrimaryPsiLanguage.Is<JsonNewLanguage>())
                return new AsmDefNameReferenceFactory();

            return null;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }
}