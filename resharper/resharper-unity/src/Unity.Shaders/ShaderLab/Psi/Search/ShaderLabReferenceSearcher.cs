using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Search
{
    public class ShaderLabReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly IDeclaredElementsSet myElements;
        private readonly ReferenceSearcherParameters myReferenceSearcherParameters;
        private readonly List<string> myElementNames;
        private readonly bool mySearchInCSharp;

        public ShaderLabReferenceSearcher(IDeclaredElementsSet elements, ReferenceSearcherParameters referenceSearcherParameters)
        {
            myElements = elements;
            myReferenceSearcherParameters = referenceSearcherParameters;

            myElementNames = new List<string>();
            foreach (var element in elements)
            {
                if (CanHaveReferenceInCSharp(element))
                    mySearchInCSharp = true;
                myElementNames.Add(element.ShortName);
            }
        }
        
        private static bool CanHaveReferenceInCSharp(IDeclaredElement declaredElement) => declaredElement is ShaderDeclaredElement;

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            var language = sourceFile.PrimaryPsiLanguage;
            var languageSupported = language.Is<ShaderLabLanguage>() || (mySearchInCSharp && language.Is<CSharpLanguage>());
            return languageSupported && sourceFile.GetPrimaryPsiFile() is { } psiFile && ProcessElement(psiFile, consumer);
        }

        public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
        {
            Assertion.AssertNotNull(element, "element != null");
            var result = new ReferenceSearchSourceFileProcessor<TResult>(element, myReferenceSearcherParameters, consumer,
                myElements, myElementNames, myElementNames).Run();
            return result == FindExecution.Stop;
        }
    }
}