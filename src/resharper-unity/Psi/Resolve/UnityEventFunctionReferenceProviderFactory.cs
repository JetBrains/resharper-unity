using System;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    [ReferenceProviderFactory]
    public class UnityEventFunctionReferenceProviderFactory : IReferenceProviderFactory
    {
#if RIDER
        public UnityEventFunctionReferenceProviderFactory(Lifetime lifetime)
        {
            Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);
        }
#endif

#if RIDER
        public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
#else
        public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file)
#endif
        {
            var project = sourceFile.GetProject();
            if (project == null || !project.IsUnityProject())
                return null;

            if (sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>())
                return new UnityEventFunctionReferenceFactory();

            return null;
        }

#if RIDER
        public ISignal<IReferenceProviderFactory> Changed { get; private set; }
#else
        public event Action OnChanged;
#endif
    }
}