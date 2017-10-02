using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    public static class CgKeywordsList
    {
        public static readonly NodeTypeSet KEYWORDS;

        // float -> float[1-4] -> float[1-4]x[1-4]
        public static readonly Dictionary<string, TokenNodeType> BuiltInTypes;

        static CgKeywordsList()
        {
            var values = typeof(CgTokenNodeTypes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.Name.EndsWith("_KEYWORD"))
                .Where(f => f.FieldType == typeof(TokenNodeType))
                .OrderBy(f => f.Name, StringComparer.InvariantCultureIgnoreCase)
                .Select(f => f.GetValue(null)).Cast<CgTokenNodeTypeBase>()
                .ToList();

            KEYWORDS = new NodeTypeSet(
                values
            );
            
            BuiltInTypes = new Dictionary<string, TokenNodeType>();
            
            // double gives error on d3d11: vs_4_0 does not support doubles as a storage type at line 34 (on d3d11)
            var builtInTypesRepresentation = new[]
            {
                "bool",
                "int",
                "uint",
                "half",
                "float",
                "double"
            };
            
            foreach (var builtInType in builtInTypesRepresentation)
            {
                BuiltInTypes.Add(builtInType, CgTokenNodeTypes.SCALAR_TYPE);
                for (var x = 1; x <= 4; x++)
                {
                    BuiltInTypes.Add($"{builtInType}{x}", CgTokenNodeTypes.VECTOR_TYPE);
                    for (var y = 1; y <= 4; y++)
                    {
                        BuiltInTypes.Add($"{builtInType}{x}x{y}", CgTokenNodeTypes.MATRIX_TYPE);
                    }
                }
            }
        }
    }
}