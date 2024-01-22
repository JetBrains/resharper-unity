#nullable enable
using System;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    public static class UnityCompletionUtils
    {
        public static bool IsSpecificArgumentInSpecificMethod(CSharpCodeCompletionContext context, out ICSharpLiteralExpression? stringLiteral, out string? typeParamName,
            Func<IInvocationExpression, bool> methodChecker, Func<IArgumentList, ICSharpArgument, bool> argumentChecker)
        {
            stringLiteral = null;
            typeParamName = null;
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
                    typeParamName = ExpressionReferenceUtils.GetInvocationTypeArgumentName(invocationExpression);
                    return true;
                }
            }

            stringLiteral = null;
            return false;
        }
        
        public static bool IsSpecificArgumentInConstructor(CSharpCodeCompletionContext context, Func<IObjectCreationExpression, bool> methodChecker, Func<IArgumentList, ICSharpArgument, bool> argumentChecker)
        {
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
                    possibleInvocationExpression = ObjectCreationExpressionNavigator.GetByArgument(argument);
                }
            }
        
            if (possibleInvocationExpression is IObjectCreationExpression invocationExpression)
            {
                if (methodChecker(invocationExpression))
                {
                    return true;
                }
            }
        
            return false;
        }


        public static Func<IArgumentList, ICSharpArgument, bool> IsCorrespondingArgument(string argumentName, int argumentIndex = 0)
        {
            return (argumentList, argument) =>
            {
                if(argument.IsNamedArgument && argument.NameIdentifier != null && argument.NameIdentifier.Name.Equals(argumentName))
                    return true;

                if (argumentList.Arguments.Count > argumentIndex && argumentList.Arguments[argumentIndex] == argument)
                    return true;
                
                return false;
            };
        }

        internal static ICSharpLiteralExpression? StringLiteral(this CSharpCodeCompletionContext context)
        {
            if (context.NodeInFile is not ITokenNode { Parent: ICSharpLiteralExpression literalExpression })
                return null;

            if (!literalExpression.IsConstantValue())
                return null;

            if (!literalExpression.ConstantValue.IsString())
                return null;

            return literalExpression;
        }
    }
}