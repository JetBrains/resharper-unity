using System;
using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search
{
    internal class CSharpInputActionsReferenceSearcher : IDomainSpecificSearcher
    {
        private readonly List<FindResultText> myResults;

        public CSharpInputActionsReferenceSearcher(List<FindResultText> results)
        {
            myResults = results;
        }

        public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
        {
            if (!sourceFile.IsInputActions()) return false;
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