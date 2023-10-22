#nullable enable

using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.Xml;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.CodeCompletion
{
    public class UxmlCodeCompletionContext : XmlCodeCompletionContext
    {
        public override string ContextId => "UxmlSpecificContext";

        public UxmlCodeCompletionContext(CodeCompletionContext context, TextLookupRanges ranges, XmlReparsedCodeCompletionContext unterminatedContext) : base(context, ranges, unterminatedContext)
        {
        }
    }
}