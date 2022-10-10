using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Search.SearchDomain;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search
{
    // from csharp to inputactions
    [PsiSharedComponent]
    public class UnityInputActionReferenceUsageSearchFactory : IDomainSpecificSearcherFactory
    {
        public bool IsCompatibleWithLanguage(PsiLanguageType languageType)
        {
            return languageType.Is<CSharpLanguage>();
        }

        public IDomainSpecificSearcher CreateConstructorSpecialReferenceSearcher(ICollection<IConstructor> constructors)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateTargetTypedObjectCreationSearcher(IReadOnlyList<IConstructor> constructors, IReadOnlyList<ITypeElement> typeElements,
            ReferenceSearcherParameters referenceSearcherParameters)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateMethodsReferencedByDelegateSearcher(IDelegate @delegate)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements,
            ReferenceSearcherParameters referenceSearcherParameters)
        {
            var declaredElements = elements.Where(e => e is IMethod && IsInterestingElement(e)).ToArray();
            
            if (!declaredElements.Any())
                return null;
        
            var solution = elements.First().GetSolution();
            var container = solution.GetComponent<InputActionsElementContainer>();

            List<FindResultText> results = new List<FindResultText>();
            foreach (var declaredElement in declaredElements)
            {
                var usages = container.GetUsagesFor(declaredElement);
                if (usages.Any())
                {
                    foreach (var usage in usages)
                    {
                        results.Add(new UnityInputActionsFindResultText(usage.SourceFile, new DocumentRange(usage.SourceFile.Document, usage.NavigationOffset)));
                    }
                }
            }
            
            return new CSharpInputActionsReferenceSearcher(results);
        }

        public IDomainSpecificSearcher CreateLateBoundReferenceSearcher(IDeclaredElementsSet elements,
            ReferenceSearcherParameters referenceSearcherParameters)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateTextOccurrenceSearcher(IDeclaredElementsSet elements)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateTextOccurrenceSearcher(string subject)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateAnonymousTypeSearcher(IList<AnonymousTypeDescriptor> typeDescription, bool caseSensitive)
        {
            return null;
        }

        public IDomainSpecificSearcher CreateConstantExpressionSearcher(ConstantValue constantValue, bool onlyLiteralExpression)
        {
            return null;
        }

        public IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            if (IsInterestingElement(element))
                yield return element.ShortName.Substring(2);
        }

        public IEnumerable<RelatedDeclaredElement> GetRelatedDeclaredElements(IDeclaredElement element)
        {
            return EmptyList<RelatedDeclaredElement>.ReadOnly;
        }

        public IEnumerable<FindResult> GetRelatedFindResults(IDeclaredElement element)
        {
            return EmptyList<FindResult>.ReadOnly;
        }

        public DerivedFindRequest GetDerivedFindRequest(IFindResultReference result)
        {
            return null;
        }

        public NavigateTargets GetNavigateToTargets(IDeclaredElement element)
        {
           return NavigateTargets.Empty;
        }

        public ICollection<FindResult>? TransformNavigationTargets(ICollection<FindResult> targets)
        {
            return null;
        }

        public ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
        {
            return EmptySearchDomain.Instance;
        }
        
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
    }

    internal class CSharpInputActionsReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly List<FindResultText> myResults;

        public CSharpInputActionsReferenceSearcher(List<FindResultText> results)
        {
            myResults = results;
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            foreach (var result in myResults)
            {
                consumer.Accept(result);
            }

            return false;
        }

        public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
        {
            throw new Exception("Unexpected call to ProcessElement");
        }
    }
    
    internal class UnityInputActionsFindResultText:FindResultText
    {
        public UnityInputActionsFindResultText(IPsiSourceFile sourceFile, DocumentRange documentRange) : base(sourceFile, documentRange)
        {
        }
    }

    internal class UnityInputActionsTextOccurence : TextOccurrence
    {
        public UnityInputActionsTextOccurence(IPsiSourceFile sourceFile, DocumentRange documentRange, OccurrencePresentationOptions presentationOptions, OccurrenceType occurrenceType = OccurrenceType.Occurrence) : base(sourceFile, documentRange, presentationOptions, occurrenceType)
        {
        }
    }

    [OccurrenceProvider(Priority = 20)]
    internal class UnityInputActionsOccurenceProvider : IOccurrenceProvider
    {
        public IOccurrence MakeOccurrence(FindResult findResult)
        {
            if (findResult is UnityInputActionsFindResultText unityInputActionsFindResultText)
            {
                return new UnityInputActionsTextOccurence(unityInputActionsFindResultText.SourceFile,
                    unityInputActionsFindResultText.DocumentRange, OccurrencePresentationOptions.DefaultOptions);
            }

            return null;
        }
    }
}