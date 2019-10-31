using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.CodeCompletion
{
    public class JsonNewCodeCompletionContext : SpecificCodeCompletionContext
    {
        public IJsonNewCodeCompletionParseContext UnterminatedContext { get; private set; }
        public TextLookupRanges Ranges { get; private set; }

        public JsonNewCodeCompletionContext([NotNull] CodeCompletionContext context, TextLookupRanges ranges,  IJsonNewCodeCompletionParseContext unterminatedContext)
            : base(context)
        {
            UnterminatedContext = unterminatedContext;
            Ranges = ranges;
        }

        public override string ContextId => "JsonNewSpecificContext";
    }
}