using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class PreferNonAllocApiQuickFix : QuickFixBase
    {
        private const string ResultParamName = "results";
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
            if (oldMethod == null)
                return null;
            
            var factory = CSharpElementFactory.GetInstance(myInvocationExpression);
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

            var newParameters = myNewMethod.Parameters;
            var argumentIndex = 0;
            
            // insert new argument to correct position. If we should use positional argument, we will check it and use.
            for (int i = 0; i < newParameters.Count; i++)
            {
                var parameter = newParameters[i];
                if (parameter.Type.Equals(returnType))
                {
                    var referenceExpression = factory.CreateReferenceExpression(GetUniqueName(myInvocationExpression, ResultParamName));
                    IArgument argument = curArgIdx > firstPositionalArgIdx
                        ? factory.CreateArgument(ParameterKind.VALUE, ResultParamName, referenceExpression)
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
            
            builder.Append(")");

            var newInvocation = factory.CreateExpression(builder.ToString(), builder.ToArguments());
            var newDeclaration = (IDeclarationStatement)factory.CreateStatement("var $0 = $1;", GetUniqueName(myInvocationExpression, "size"), newInvocation);

            var oldStatement = myInvocationExpression.GetContainingStatement().NotNull("oldStatement != null");

            IDeclarationStatement result;
            if (oldStatement is IExpressionStatement)
            {
                result = oldStatement.ReplaceBy(newDeclaration);
            }
            else
            {
                var declaration = (oldStatement as IDeclarationStatement).NotNull(nameof(oldStatement) + " is not IDeclarationStatement");
                
                // if only one declaration just replace it
                if (declaration.Declaration.Declarators.Count == 1)
                {
                    result = oldStatement.ReplaceBy(newDeclaration);
                }
                else
                {
                    // There are several declaration, exclude our and transform it.
                    var expression = myInvocationExpression.GetContainingParenthesizedExpression();
                    var currentInitializer = ExpressionInitializerNavigator.GetByValue(expression);
                    var selectedDeclarator = LocalVariableDeclarationNavigator.GetByInitial(currentInitializer).NotNull("selectedDeclarator != null");

                    MultipleDeclarationUtil.SplitDeclarationBefore(selectedDeclarator);
                    MultipleDeclarationUtil.SplitDeclarationAfter(selectedDeclarator);

                    result = declaration.ReplaceBy(newDeclaration);
                }
            }
            
            var actualArgument = result.Descendants<IArgumentList>().First().Arguments[argumentIndex];
            if (!actualArgument.IsValid())
                return null;
            
            // allow user to decide which array he will use
            var hotspotsRegistry = new HotspotsRegistry(newInvocation.GetPsiServices());
            hotspotsRegistry.Register(new ITreeNode[] {actualArgument});

            return BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, actualArgument.GetDocumentRange());
        }


        private static string GetUniqueName([NotNull]IInvocationExpression invocationExpression,[NotNull] string baseName)
        {
            var namingManager = invocationExpression.GetPsiServices().Naming;

            var policyProvider = namingManager.Policy.GetPolicyProvider(invocationExpression.Language, invocationExpression.GetSourceFile());
            var namingRule = policyProvider.GetPolicy(NamedElementKinds.Locals).NamingRule;
            var name = namingManager.Parsing.Parse(baseName, namingRule, policyProvider);
            var nameRoot = name.GetRootOrDefault(baseName);
            var namesCollection = namingManager.Suggestion.CreateEmptyCollection(PluralityKinds.Unknown, CSharpLanguage.Instance, true, policyProvider);
            namesCollection.Add(nameRoot, new EntryOptions(PluralityKinds.Unknown, SubrootPolicy.Decompose, emphasis: Emphasis.Good));

            var suggestionOptions = new SuggestionOptions
            {
                DefaultName = baseName,
                UniqueNameContext = invocationExpression,
            };

            var namesSuggestion = namesCollection.Prepare(NamedElementKinds.Locals, ScopeKind.Common, suggestionOptions);
            return namesSuggestion.FirstName();
        }

        private int GetPositionalArgumentIndex(TreeNodeCollection<ICSharpArgument> invocationExpressionArguments)
        {            
            var i = 0;
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
            return ValidUtils.Valid(myInvocationExpression) && (IsExpressionStatement() || IsInvocationInitializer()) 
                                                  && !myExpression.ContainsPreprocessorDirectives();
        }

        private bool IsExpressionStatement()
        {
            var expression = myInvocationExpression.GetContainingParenthesizedExpression();
            return ExpressionStatementNavigator.GetByExpression(expression) != null;
        }

        // Check that invocation immediately under declaration statement as initializer (Parenthesized Expression is allowed)
        private bool IsInvocationInitializer()
        {
            var expression = myInvocationExpression.GetContainingParenthesizedExpression();
            var currentInitializer = ExpressionInitializerNavigator.GetByValue(expression);
            var selectedDeclarator = LocalVariableDeclarationNavigator.GetByInitial(currentInitializer);
            var multiplyVariableDeclaration = MultipleLocalVariableDeclarationNavigator.GetByDeclarator(selectedDeclarator);
            
            return DeclarationStatementNavigator.GetByDeclaration(multiplyVariableDeclaration) != null;
        }
    }
}