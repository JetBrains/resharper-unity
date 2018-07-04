using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExpectedTypes;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    // Removes everything apart from name from completion items for event functions in string literals
    [Language(typeof(CSharpLanguage))]
    public class UnityReferenceFromStringLiteralTransformationRule : ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
    {
        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.TerminatedContext.Reference is IUnityReferenceFromStringLiteral;
        }

        protected override AutocompletionBehaviour GetAutocompletionBehaviour(CSharpCodeCompletionContext specificContext)
        {
            return AutocompletionBehaviour.AutocompleteWithReplace;
        }

        protected override void DecorateItems(CSharpCodeCompletionContext context, IEnumerable<ILookupItem> items)
        {
            if (!IsAvailable(context))
                return;

            foreach (var item in items)
            {
                if (item.IsMethodsLookupItem())
                {
                    // Remove the trailing parentheses from the item
                    item.EraseInsertText();
                    item.EraseReplaceText();
                    item.SetTailType(TailType.None);
                }
            }
        }
    }
}