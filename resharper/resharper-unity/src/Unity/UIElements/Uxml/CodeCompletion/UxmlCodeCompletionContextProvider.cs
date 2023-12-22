#nullable enable

using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.Xml;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.CodeCompletion
{
    [IntellisensePart]
    public class UxmlCodeCompletionContextProvider : XmlCodeCompletionContextProvider
    {
        private readonly UxmlCodeCompletionManager myManager;

        public UxmlCodeCompletionContextProvider(UxmlCodeCompletionManager manager)
        {
            myManager = manager;
        }
        
        protected override IXmlFile? IsAvailableImpl(CodeCompletionContext context)
        {
            if (context.Language.Is<UxmlLanguage>())
                return context.File as IXmlFile;
            return null;
        }

        protected override ISpecificCodeCompletionContext CreateSpecificCompletionContext(
            CodeCompletionContext context, TextLookupRanges ranges, XmlReparsedCodeCompletionContext unterminatedContext)
        {
            return new UxmlCodeCompletionContext(context, CalculateDefaultRanges(context), unterminatedContext);
        }
    }
}