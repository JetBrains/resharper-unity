using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Web.WebConfig;

namespace JetBrains.ReSharper.Plugins.Unity.Json
{
    public static class Extensions
    {
        public static bool IsAsmDef(this IPsiSourceFile sourceFile)
        {
            return sourceFile.GetExtensionWithDot().Equals(".asmdef", StringComparison.InvariantCultureIgnoreCase);
        }

        [ContractAnnotation("node:null => false")]
        public static bool IsNameStringLiteralValue([CanBeNull] this ITreeNode node)
        {
            if (node is IJavaScriptLiteralExpression literal && literal.IsStringLiteral())
            {
                var file = node.GetContainingFile();
                var initializer = ObjectPropertyInitializerNavigator.GetByValue(literal);
                var expectedFile = GetByRootObjectPropertyInitializer(initializer);

                if (expectedFile == file && initializer?.DeclaredName == "name")
                    return true;
            }

            return false;
        }

        [ContractAnnotation("node:null => false")]
        public static bool IsReferencesStringLiteralValue([CanBeNull] this ITreeNode node)
        {
            if (node is IJavaScriptLiteralExpression literal && literal.IsStringLiteral())
            {
                var file = node.GetContainingFile();
                var arrayLiteral = ArrayLiteralNavigator.GetByArrayElement(literal);
                var initializer = ObjectPropertyInitializerNavigator.GetByValue(arrayLiteral);
                var expectedFile = GetByRootObjectPropertyInitializer(initializer);

                if (expectedFile == file && initializer?.DeclaredName == "references")
                    return true;
            }

            return false;
        }

        private static IJavaScriptFile GetByRootObjectPropertyInitializer(IObjectPropertyInitializer initializer)
        {
            var objectLiteral = ObjectLiteralNavigator.GetByPropertie(initializer);
            var compoundExpression = CompoundExpressionNavigator.GetByExpression(objectLiteral);
            var statement = ExpressionStatementNavigator.GetByExpression(compoundExpression);
            return JavaScriptFileNavigator.GetByAllStatement(statement);
        }
    }
}