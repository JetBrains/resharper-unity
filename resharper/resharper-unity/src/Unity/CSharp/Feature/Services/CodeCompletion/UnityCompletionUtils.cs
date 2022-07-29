using System;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    internal static class UnityCompletionUtils
    {
        internal static bool IsSpecificArgumentInSpecificMethod(CSharpCodeCompletionContext context, out ICSharpLiteralExpression stringLiteral,
            Func<IInvocationExpression, bool> methodChecker, Func<IArgumentList, ICSharpArgument, bool> argumentChecker)
        {
            stringLiteral = null;
            var nodeInFile = context.NodeInFile as ITokenNode;
            if (nodeInFile == null)
                return false;

            var possibleInvocationExpression = nodeInFile.Parent;
            if (possibleInvocationExpression is ICSharpLiteralExpression literalExpression)
            {
                if (!literalExpression.Literal.IsAnyStringLiteral())
                    return false;

                var argument = CSharpArgumentNavigator.GetByValue(literalExpression);
                var argumentList = ArgumentListNavigator.GetByArgument(argument);
                if (argument == null || argumentList == null)
                    return false;

                if (argumentChecker(argumentList, argument))
                {
                    stringLiteral = literalExpression;
                    possibleInvocationExpression = InvocationExpressionNavigator.GetByArgument(argument);
                }
            }

            if (possibleInvocationExpression is IInvocationExpression invocationExpression)
            {
                if (methodChecker(invocationExpression))
                {
                    return true;
                }
            }

            stringLiteral = null;
            return false;
        }

        public static Func<IArgumentList, ICSharpArgument, bool> IsCorrespondingArgument(string argumentName)
        {
            return (argumentList, argument) => argument.IsNamedArgument && argument.NameIdentifier.Name.Equals(argumentName) ||
                                               !argument.IsNamedArgument && argumentList.Arguments[0] == argument;
        }
    }
}