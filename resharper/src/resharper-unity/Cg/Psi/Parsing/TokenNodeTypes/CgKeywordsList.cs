using System;
using System.Linq;
using System.Reflection;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    public static class CgKeywordsList
    {
        public static readonly NodeTypeSet ALL;

        static CgKeywordsList()
        {
            var fields = typeof(CgTokenNodeTypes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.Name.EndsWith("_KEYWORD") && f.FieldType == typeof(CgKeywordTokenNodeType))
                .OrderBy(f => f.Name, StringComparer.InvariantCultureIgnoreCase);

            var values = fields.Select(f => f.GetValue(null)).Cast<CgKeywordTokenNodeType>().ToArray();
            
            ALL = new NodeTypeSet(
                values
            );
        }
    }
}