using JetBrains.ReSharper.Feature.Services.Daemon.IdeaAttributes;
using JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages;
using JetBrains.TextControl.DocumentMarkup;

[assembly: 
    RegisterHighlighter(CgHighlightingAttributeIds.KEYWORD, FallbackAttributeId = IdeaHighlightingAttributeIds.KEYWORD, )

]
namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    public static class CgHighlightingAttributeIds
    {
        public const string KEYWORD = "ReSharper Cg Keyword";
        public const string NUMBER = "ReSharper Cg Number";
        public const string LINE_COMMENT = "ReSharper Cg Line_comment";
        public const string DELIMETED_COMMENT  = "ReSharper Cg Delimeted_comment";

        public const string FIELD_IDENTIFIER = "ReSharper Cg Field_identifier";
        public const string METHOD_IDENTIFIER = "ReSharper Cg Method_identifier";

        public const string TYPE_STRUCT = "ReSharper Cg Type_struct";
        public const string TYPE_CLASS = "ReSharper Cg Type_class";

        public const string PARAMETER_IDENTIFIER = "ReSharper Cg Parameter_identifier";
        public const string CPP_MACRO_NAME = "ReSharper Cg Cpp_macro_name";
        public const string LOCAL_VARIABLE_IDENTIFIER = "ReSharper Cg Local_variable_identifier";
    }
}