using System;
using System.Text;
using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Presentation;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.UI.RichText;
using JetBrains.Util.Media;

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
                var usages = myContainer.GetUsagesFor(jsonSourceFile, declaredElement);
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

    [OccurrencePresenter(Priority = 10.0)]
    internal class UnityInputActionsTextOccurencePresenter : RangeOccurrencePresenter
    {
        public override bool Present(IMenuItemDescriptor descriptor, IOccurrence occurrence,
            OccurrencePresentationOptions occurrencePresentationOptions)
        {
            var result = base.Present(descriptor, occurrence, occurrencePresentationOptions);
            var inputActionsOccurrence = (occurrence as UnityInputActionsTextOccurence).NotNull("occurrence as UnityInputActionsTextOccurence != null");
            AppendRelatedFolder(descriptor, inputActionsOccurrence.GetRelatedFolderPresentation());
            descriptor.Icon = inputActionsOccurrence.GetIcon();
            return result;
        }
        
        public static void AppendRelatedFolder(IMenuItemDescriptor descriptor, string relatedFolderPresentation)
        {
            var sb = new StringBuilder();
            
            if (relatedFolderPresentation != null)
            {
                sb.Append($"in {relatedFolderPresentation}");
            }
            
            descriptor.ShortcutText = new RichText(sb.ToString(), TextStyle.FromForeColor(JetRgbaColors.DarkGray));
        }
        
        public override bool IsApplicable(IOccurrence occurrence)
        {
            return occurrence is UnityInputActionsTextOccurence;
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