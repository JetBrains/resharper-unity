using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.CodeCompletion
{
    public class JsonNewCodeCompletionContext : SpecificCodeCompletionContext
    {
        public IJsonNewCodeCompletionParseContext UnterminatedContext { get; }
        public TextLookupRanges Ranges { get; }

        public JsonNewCodeCompletionContext(CodeCompletionContext context, TextLookupRanges ranges,
                                            IJsonNewCodeCompletionParseContext unterminatedContext)
            : base(context)
        {
            UnterminatedContext = unterminatedContext;
            Ranges = ranges;
        }

        public override string ContextId => "JsonNewSpecificContext";
    }
}