#nullable enable

using JetBrains.Application.Parts;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    [ReferenceProviderFactory(Instantiation.DemandAnyThreadSafe)]
    public class SyncVarHookReferenceProviderFactory : IReferenceProviderFactory
    {
        public SyncVarHookReferenceProviderFactory()
        {
            Changed = new Signal<IReferenceProviderFactory>(GetType().FullName!);
        }

        public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex? wordIndexForChecks)
        {
            var project = sourceFile.GetProject();
            if (project == null || !project.IsUnityProject())
                return null;

            if (sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>())
                return new SyncVarHookReferenceFactory();

            return null;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }
}
