using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal static class ShaderLabElementFactory
    {
        public static CompositeElement CreateErrorElement(SyntaxError error)
        {
            if (error is IUnexpectedTokenError unexpectedToken)
                return new UnexpectedTokenErrorElement(unexpectedToken.TokenType, unexpectedToken.ExpectedTokenTypes, unexpectedToken.Message);
            return TreeElementFactory.CreateErrorElement(error.Message);
        }
    }
}