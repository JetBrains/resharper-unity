using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.Search.Operations;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class PreferNonAllocApiQuickFix : QuickFixBase
    {
        private readonly IReferenceExpression myExpression;
        private readonly IMethod myNewMethod;
        private readonly IInvocationExpression myInvocationExpression;

        public PreferNonAllocApiQuickFix(PreferNonAllocApiWarning warning)
        {
            myInvocationExpression = warning.InvocationExpression;
            myExpression = warning.InvokedExpression;
            myNewMethod = warning.NonAllocMethod;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var oldMethod = myExpression.Reference.Resolve().DeclaredElement as IMethod;
            var factory = CSharpElementFactory.GetInstance(myInvocationExpression);

            if (oldMethod == null)
            {
                return null;
            }

            var builder = FactoryArgumentsBuilder.Create();

            if (myExpression.QualifierExpression != null)
            {
                builder.Argument(myExpression.QualifierExpression);
                builder.Append(".");
            }

            builder.Append(myNewMethod.ShortName);
            builder.Append("(");
            var returnType = oldMethod.ReturnType;

            var oldArguments = myInvocationExpression.Arguments;

            var firstPositionalArgIdx = GetPositionalArgumentIndex(oldArguments);
            var curArgIdx = 0;

            IArgument argument = null;
            var newParameters = myNewMethod.Parameters;
            var argumentIndex = 0;
            for (int i = 0; i < newParameters.Count; i++)
            {
                var parameter = newParameters[i];
                if (parameter.Type.Equals(returnType))
                {
                    var referenceExpression = GetUniqueName(factory, "results");
                    argument = curArgIdx >= firstPositionalArgIdx
                        ? factory.CreateArgument(ParameterKind.VALUE, "results", referenceExpression)
                        : factory.CreateArgument(ParameterKind.VALUE, referenceExpression);
                    builder.Argument(argument);
                    argumentIndex = i;
                }
                else
                {
                    builder.Argument(oldArguments[curArgIdx]);
                    curArgIdx++;
                }

                if (i + 1 != newParameters.Count)
                {
                    builder.Append(",");
                }
            }

            if (argument == null)
            {
                var logger = solution.GetComponent<ILogger>();
                logger.Log(LoggingLevel.WARN, "Expected to insert result argument, but position for argument was not found.");
                return null;
            }
            
            builder.Append(")");

            var newInvocation = factory.CreateExpression(builder.ToString(), builder.ToArguments());
            var declaration = factory.CreateStatement($"int {GetUniqueName(factory, "size").NameIdentifier.Name} = $0;", newInvocation);
            
            var result = myInvocationExpression.GetContainingStatement()?.ReplaceBy(declaration);

            if (result == null)
            {
                return null;
            }

            var actualArgument = result.Descendants<IArgumentList>().First().Arguments[argumentIndex];
            if (actualArgument == null)
            {
                var logger = solution.GetComponent<ILogger>();
                logger.Log(LoggingLevel.WARN, "Actual argument was not found.");
                return null;
            }
            
            var hotspotsRegistry = new HotspotsRegistry(newInvocation.GetPsiServices());
            hotspotsRegistry.Register(new ITreeNode[] {actualArgument});

            return BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, actualArgument.GetDocumentRange());
        }

        private int GetPositionalArgumentIndex(in TreeNodeCollection<ICSharpArgument> invocationExpressionArguments)
        {
            int i = 0;
            for (; i < invocationExpressionArguments.Count; i++)
            {
                var argument = invocationExpressionArguments[i];
                if (argument.NameIdentifier != null)
                {
                    break;
                }
            }

            return i;
        }

        public override string Text => $"Convert to {myNewMethod.ShortName}";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return ValidUtils.Valid(myExpression) && (IsInvocationExpressionStatement() || IsInvocationInitializer()) 
                                                  && !myExpression.ContainsPreprocessorDirectives();
        }


        private IReferenceExpression GetUniqueName(CSharpElementFactory factory, string name)
        {
            var originName = name;
            var curIdx = 1;
            while (true)
            {
                var referenceExpression = factory.CreateReferenceExpression(name);
                if (referenceExpression.Reference.Resolve().ResolveErrorType == ResolveErrorType.OK)
                {
                    name = originName + curIdx++;
                }
                else
                {
                    return factory.CreateReferenceExpression(name);
                }
            }
        }

        private bool IsInvocationExpressionStatement()
        {
            ITreeNode current = myInvocationExpression;
            while (true)
            {
                if (current.Parent == null)
                {
                    return false;
                }

                current = current.Parent;

                if (current is IExpressionStatement)
                {
                    return true;
                }
                
                if (!(current is IParenthesizedExpression))
                {
                    return false;
                }
            }
        }

        private bool IsInvocationInitializer()
        {
            ITreeNode current = myInvocationExpression;

            while (true)
            {
                if (current.Parent == null)
                {
                    return false;
                }

                current = current.Parent;

                if (current is IExpressionInitializer)
                {
                    break;
                }

                if (!(current is IParenthesizedExpression))
                {
                    break;
                }
            }

            if (!(current.Parent is ILocalVariableDeclaration))
            {
                return false;
            }
            current = current.Parent;
            
            if (!(current.Parent is ILocalVariableDeclaration))
            {
                return false;
            }
            current = current.Parent;
            
            if (!(current.Parent is IMultipleLocalVariableDeclaration))
            {
                return false;
            }
            
            return current.Parent is IDeclarationStatement;
        }
    }
}