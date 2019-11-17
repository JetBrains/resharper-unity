using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree
{
    public partial interface IJsonNewLiteralExpression
    {
        ConstantValueTypes ConstantValueType { get; }
        TreeTextRange GetInnerTreeTextRange();
        string GetStringValue();
    }
}