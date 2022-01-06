using JetBrains.ReSharper.Psi;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree
{
    public partial interface IJsonNewLiteralExpression
    {
        ConstantValueTypes ConstantValueType { get; }
        TreeTextRange GetInnerTreeTextRange();
        string? GetStringValue();
    }
}