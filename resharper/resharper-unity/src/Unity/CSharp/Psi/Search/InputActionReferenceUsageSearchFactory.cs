#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search
{
    // from csharp to inputactions
    [PsiSharedComponent]
    public class UnityInputActionReferenceUsageSearchFactory : DomainSpecificSearcherFactoryBase
    {
        public override bool IsCompatibleWithLanguage(PsiLanguageType languageType) =>
            languageType.Is<CSharpLanguage>();
        
        public static bool IsInterestingElement(IDeclaredElement? element)
        {
            if (element is null) return false;
            if (element is IMethod method)
            {
                if (method.ShortName.StartsWith("On"))
                {
                    return method.ContainingType is IClass classType && classType.DerivesFromMonoBehaviour();
                }
                return false;
            }

            return false;
            // todo: is instance method
        }

        public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement? element)
        {
            if (IsInterestingElement(element))
                yield return element.ShortName.Substring(2);
        }

        public override IDomainSpecificSearcher? CreateReferenceSearcher(
            IDeclaredElementsSet elements, ReferenceSearcherParameters referenceSearcherParameters)
        {
            if (elements.Any(e => e is not IMethod))
                return null;

            var solution = elements.First().GetSolution();
            var container = solution.GetComponent<InputActionsElementContainer>();

            return new CSharpInputActionsReferenceSearcher(container, elements.Where(e => e is IMethod && IsInterestingElement(e)));
        }
    }

    internal class CSharpInputActionsReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly InputActionsElementContainer myInputActionsElementContainer;
        private readonly IEnumerable<IDeclaredElement> myDeclaredElements;

        public CSharpInputActionsReferenceSearcher(InputActionsElementContainer inputActionsElementContainer,
            IEnumerable<IDeclaredElement> declaredElements)
        {
            myInputActionsElementContainer = inputActionsElementContainer;
            myDeclaredElements = declaredElements;
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            //return sourceFile.GetPsiFiles<CSharpLanguage>().Any(file => ProcessElement(file, consumer));
            foreach (var declaredElement in myDeclaredElements)
            {
                var usages = myInputActionsElementContainer.GetUsagesFor(declaredElement);
                if (usages.Any())
                {
                    foreach (var usage in usages)
                    {
                        consumer.Accept(new UnityInputActionToCSharpFindResult(usage));
                    }
                }
            }

            return false;
        }

        public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
        {
            throw new Exception("Unexpected");
            // if (element is IMethodDeclaration methodDeclaration && UnityInputActionReferenceUsageSearchFactory.IsInterestingElement(methodDeclaration.DeclaredElement))
            // {
            //     var usages = myInputActionsElementContainer.GetUsagesFor(methodDeclaration.DeclaredElement);
            //     if (usages.Any())
            //     {
            //         foreach (var usage in usages)
            //         {
            //             consumer.Build(new FindResultDeclaredElement(usage));
            //         }
            //     }
            // }
            //
        }
    }
    
    public class UnityInputActionToCSharpFindResult : FindResultDeclaredElement
    {
        public UnityInputActionToCSharpFindResult(IDeclaredElement declaredElement)
            : base(declaredElement) { }
        
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    
    [OccurrenceProvider(Priority = 20)]
    public class UnityInputActionsToCSharpOccurenceProvider : IOccurrenceProvider
    {
        public IOccurrence MakeOccurrence(FindResult findResult)
        {

            if (findResult is UnityInputActionToCSharpFindResult result)
            {
                return new DeclaredElementOccurrence(result.DeclaredElement, OccurrenceType.Occurrence);
            }

            return null;
        }
    }
}