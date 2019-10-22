using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.AsmdefNew
{
    public static class Extensions
    {
        public static bool IsAsmDef(this IPsiSourceFile sourceFile)
        {
            return sourceFile.GetLocation().ExtensionWithDot.Equals(".asmdef", StringComparison.InvariantCultureIgnoreCase);
        }

        [ContractAnnotation("node:null => false")]
        public static bool IsNameLiteral([CanBeNull] this ITreeNode node)
        {
            if (node is IJsonNewLiteralExpression literal && literal.ConstantValueType == ConstantValueTypes.String)
            {
                var key = JsonNewMemberNavigator.GetByValue(literal)?.Key;

                if (key == "name")
                    return true;
            }

            return false;
        }

        [ContractAnnotation("node:null => false")]
        public static bool IsReferenceLiteral([CanBeNull] this ITreeNode node)
        {
            if (node is IJsonNewLiteralExpression literal && literal.ConstantValueType == ConstantValueTypes.String)
            {
                var file = node.GetContainingFile();
                var arrayLiteral = JsonNewArrayNavigator.GetByValue(literal);
                var key = JsonNewMemberNavigator.GetByValue(arrayLiteral)?.Key;

                if (key == "references")
                    return true;
            }

            return false;
        }
    }
}