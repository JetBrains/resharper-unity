using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes
{
    public static class CgKeywordsList
    {
        public static readonly NodeTypeSet ALL;

        static CgKeywordsList()
        {
            ALL = new NodeTypeSet(
                CgTokenNodeTypes.STRUCT_KEYWORD,
                
                CgTokenNodeTypes.BOOL_KEYWORD,
                CgTokenNodeTypes.INT_KEYWORD,
                CgTokenNodeTypes.UINT_KEYWORD,
                CgTokenNodeTypes.DWORD_KEYWORD,
                CgTokenNodeTypes.HALF_KEYWORD,
                CgTokenNodeTypes.FLOAT_KEYWORD,
                CgTokenNodeTypes.DOUBLE_KEYWORD,
                
                CgTokenNodeTypes.VOID_KEYWORD,
                
                CgTokenNodeTypes.FALSE_KEYWORD,
                CgTokenNodeTypes.TRUE_KEYWORD
            );
        }
    }
}