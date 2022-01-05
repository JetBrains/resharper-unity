using JetBrains.ReSharper.Psi;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree
{
    public partial interface IJsonNewLiteralExpression
    {
        ConstantValueTypes ConstantValueType { get; }
        TreeTextRange GetInnerTreeTextRange();
        string? GetStringValue();
    }
}