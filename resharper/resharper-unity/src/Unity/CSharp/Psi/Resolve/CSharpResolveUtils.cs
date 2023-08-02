#nullable enable
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public static class CSharpResolveUtils
    {
        public static IInvocationExpression? TryGetInvocationByArgumentValue(ICSharpExpression? expression, out ICSharpArgument? argument)
        {
            argument = CSharpArgumentNavigator.GetByValue(expression);
            var argumentsOwner = CSharpArgumentsOwnerNavigator.GetByArgument(argument);
            return argumentsOwner as IInvocationExpression;
        }

        public static IMethod? TryGetInvokedMethod(this IInvocationExpression invocationExpression) => 
            invocationExpression.Reference.Resolve().DeclaredElement as IMethod;
    }
}