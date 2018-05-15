using JetBrains.DataFlow;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Psi.Resolve
{
    // Creates a dummy reference on the "name" string literal value. This works around an issue when renaming that the
    // string literal values for asmdef "name" declarations is treated as a text occurrence, and ReSharper will try to
    // rename it, but we already handle renaming it and Rider throws an exception. This happens because the
    // JavaScritpTextOccurrenceSearcher will treate any string literal node as text unless it's either contained by an
    // IDeclaration node, or contains a reference. We can't change the tree, so we have to add a reference.
    [ReferenceProviderFactory]
    public class AsmDefNameDummyReferenceProviderFactory : IReferenceProviderFactory
    {
        public AsmDefNameDummyReferenceProviderFactory(Lifetime lifetime)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);
        }

        public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
        {
            var project = sourceFile.GetProject();
            if (project == null || !project.IsUnityProject())
                return null;

            if (sourceFile.IsAsmDef() && sourceFile.PrimaryPsiLanguage.Is<JsonLanguage>())
                return new AsmDefNameDummyReferenceFactory();

            return null;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }
}
