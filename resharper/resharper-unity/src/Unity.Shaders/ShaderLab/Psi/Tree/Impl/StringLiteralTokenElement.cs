using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;

internal class StringLiteralTokenElement(TokenNodeType type, string text)
    : ShaderLabTokenType.GenericTokenElement(type, text), ILiteralExpression
{
    private readonly string myText = text;

    public ConstantValue ConstantValue => ConstantValue.String(myText, GetPsiModule());

    public ExpressionAccessType GetAccessType() => ExpressionAccessType.Read;

    public bool IsConstantValue() => true;

    public IType Type()
    {
        var predefinedType = GetPsiModule().GetPredefinedType();
        return predefinedType.String;
    }

    public IExpressionType GetExpressionType() => Type();

    public IType GetImplicitlyConvertedTo() => Type();

    public ITokenNode Literal => this;
}