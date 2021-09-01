using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef
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
                var member = JsonNewMemberNavigator.GetByValue(literal);
                var key = member?.Key;

                var file = JsonNewFileNavigator.GetByValue(JsonNewObjectNavigator.GetByMember(member));
                if (file == null)
                    return false;

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
                var arrayLiteral = JsonNewArrayNavigator.GetByValue(literal);
                var member = JsonNewMemberNavigator.GetByValue(arrayLiteral);
                var key = member?.Key;

                var file = JsonNewFileNavigator.GetByValue(JsonNewObjectNavigator.GetByMember(member));
                if (file == null)
                    return false;

                if (key == "references")
                    return true;
            }

            return false;
        }
    }
}