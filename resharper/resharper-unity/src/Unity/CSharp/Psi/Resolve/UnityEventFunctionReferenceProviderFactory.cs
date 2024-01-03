﻿#nullable enable

using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    [ReferenceProviderFactory]
    public class UnityEventFunctionReferenceProviderFactory : IReferenceProviderFactory
    {
        private readonly IPredefinedTypeCache myPredefinedTypeCache;

        public UnityEventFunctionReferenceProviderFactory(IPredefinedTypeCache predefinedTypeCache)
        {
            myPredefinedTypeCache = predefinedTypeCache;
            Changed = new Signal<IReferenceProviderFactory>(GetType().FullName!);
        }

        public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex? wordIndexForChecks)
        {
            var project = sourceFile.GetProject();
            if (project == null || !project.IsUnityProject())
                return null;

            if (sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>())
                return new UnityEventFunctionReferenceFactory(myPredefinedTypeCache);

            return null;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }
}
