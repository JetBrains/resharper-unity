using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors
{
    public static class CSharpErrorUtil
    {
        public static DocumentRange GetMethodNameAndParametersHighlightingRange(IMethodDeclaration methodDeclaration)
        {
            var nameRange = methodDeclaration.GetNameDocumentRange();
            if (!nameRange.IsValid())
                return DocumentRange.InvalidRange;

            var rpar = methodDeclaration.RPar;
            if (rpar == null)
                return nameRange;

            var rparRange = rpar.GetDocumentRange();
            if (!rparRange.IsValid() || nameRange.Document != rparRange.Document)
                return nameRange;

            return new DocumentRange(nameRange.Document,
                new TextRange(nameRange.TextRange.StartOffset, rparRange.TextRange.EndOffset));
        }

        public static DocumentRange GetParametersHighlightingRange(IMethodDeclaration methodDeclaration)
        {
            var nameRange = methodDeclaration.GetNameDocumentRange();
            if (!nameRange.IsValid())
                return DocumentRange.InvalidRange;

            var @params = methodDeclaration.Params;
            if (@params == null)
                return nameRange;

            var paramsRange = @params.GetDocumentRange();
            if (!paramsRange.IsValid())
                return nameRange;

            if (!paramsRange.IsEmpty)
                return paramsRange;

            var lparRange = methodDeclaration.LPar?.GetDocumentRange();
            var rparRange = methodDeclaration.RPar?.GetDocumentRange();
            var startOffset = lparRange != null && lparRange.Value.IsValid()
                ? lparRange.Value
                : paramsRange;
            var endOffset = rparRange != null && rparRange.Value.IsValid()
                ? rparRange.Value
                : paramsRange;

            return new DocumentRange(startOffset.StartOffset, endOffset.EndOffset);
        }

        public static DocumentRange GetReturnTypeHighlightingRange(IMethodDeclaration methodDeclaration)
        {
            var nameRange = methodDeclaration.GetNameDocumentRange();
            if (!nameRange.IsValid())
                return DocumentRange.InvalidRange;

            var returnType = methodDeclaration.TypeUsage;
            if (returnType == null)
                return nameRange;

            var documentRange = returnType.GetDocumentRange();
            return !documentRange.IsValid() ? nameRange : documentRange;
        }

        public static DocumentRange GetStaticModifierOrMethodNameHighlightingRange(IMethodDeclaration methodDeclaration)
        {
            if (methodDeclaration.IsStatic)
            {
                var modifiersList = methodDeclaration.ModifiersList;
                foreach (var modifier in modifiersList.Modifiers)
                {
                    if (modifier.NodeType == CSharpTokenType.STATIC_KEYWORD)
                        return modifier.GetDocumentRange();
                }

                var modifiersListRange = modifiersList.GetDocumentRange();
                if (modifiersListRange.IsValid() && !modifiersListRange.IsEmpty)
                    return modifiersListRange;
            }

            return methodDeclaration.GetNameDocumentRange();
        }

        public static DocumentRange GetTypeParametersOrMethodNameHighlightingRange(IMethodDeclaration methodDeclaration)
        {
            var typeParameterList = methodDeclaration.TypeParameterList;
            if (typeParameterList == null)
                return methodDeclaration.GetNameDocumentRange();

            var documentRange = typeParameterList.GetDocumentRange();
            return documentRange.IsValid() ? documentRange : methodDeclaration.GetNameDocumentRange();
        }
    }
}