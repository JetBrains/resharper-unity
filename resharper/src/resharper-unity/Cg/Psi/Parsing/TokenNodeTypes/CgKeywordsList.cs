using System;
using System.Linq;
using System.Reflection;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    public static class CgKeywordsList
    {
        public static readonly NodeTypeSet ALL;

        static CgKeywordsList()
        {
            var values = typeof(CgTokenNodeTypes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.Name.EndsWith("_KEYWORD"))
                .Where(f => f.FieldType == typeof(TokenNodeType))
                .OrderBy(f => f.Name, StringComparer.InvariantCultureIgnoreCase)
                .Select(f => f.GetValue(null)).Cast<CgKeywordTokenNodeType>()
                .ToArray();
            
            ALL = new NodeTypeSet(
                values
            );
        }
    }
}