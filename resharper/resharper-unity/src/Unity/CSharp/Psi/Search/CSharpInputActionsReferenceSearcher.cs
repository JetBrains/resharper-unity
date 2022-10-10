using System;
using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search
{
    internal class CSharpInputActionsReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly DeclaredElementsSet myCsharpDeclaredElements;
        private readonly InputActionsElementContainer myContainer;

        public CSharpInputActionsReferenceSearcher(DeclaredElementsSet csharpDeclaredElements, InputActionsElementContainer container)
        {
            myCsharpDeclaredElements = csharpDeclaredElements;
            myContainer = container;
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile jsonSourceFile, IFindResultConsumer<TResult> consumer)
        {
            if (!jsonSourceFile.IsInputActions()) return false;
            
            foreach (var declaredElement in myCsharpDeclaredElements)
            {
                var usages = myContainer.GetUsagesFor(declaredElement);
                foreach (var usage in usages)
                {
                    consumer.Accept(new UnityInputActionsFindResultText(usage.SourceFile,
                        new DocumentRange(usage.SourceFile.Document, usage.NavigationOffset)));
                }
            }
            
            return false;
        }

        public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
        {
            throw new Exception("Unexpected call to ProcessElement");
        }
    }


    internal class UnityInputActionsFindResultText : FindResultText
    {
        public UnityInputActionsFindResultText(IPsiSourceFile sourceFile, DocumentRange documentRange) : base(
            sourceFile, documentRange)
        {
        }
    }

    internal class UnityInputActionsTextOccurence : TextOccurrence
    {
        public UnityInputActionsTextOccurence(IPsiSourceFile sourceFile, DocumentRange documentRange,
            OccurrencePresentationOptions presentationOptions,
            OccurrenceType occurrenceType = OccurrenceType.Occurrence) : base(sourceFile, documentRange,
            presentationOptions, occurrenceType)
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