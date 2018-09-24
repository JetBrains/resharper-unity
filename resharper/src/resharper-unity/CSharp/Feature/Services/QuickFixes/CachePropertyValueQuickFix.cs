using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
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
    public class CachePropertyValueQuickFix : QuickFixBase
    {
        private readonly IReferenceExpression[] myReferences;
        private readonly IReferenceExpression myHighlightedReference;
        private readonly bool myHasRead;
        private readonly bool myHasWrite;
        
        public CachePropertyValueQuickFix(InefficientPropertyAccessWarning warning)
        {
            myReferences = warning.References;
            myHighlightedReference = warning.HighlightedReference;
            myHasRead = warning.HasRead;
            myHasWrite = warning.HasWrite;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var factory = CSharpElementFactory.GetInstance(myHighlightedReference);
            var property = (myHighlightedReference.Reference.Resolve().DeclaredElement as IProperty).NotNull();

            // save declaration (in case of expression lambda)
            var declaration = myHighlightedReference.GetContainingFunctionLikeDeclarationOrClosure();
            var type = property.Type;

            var name = GetUniqueName(myHighlightedReference, property.ShortName);

            IReferenceExpression originValue = myHighlightedReference.Copy();
            
            for (var i = 0; i < myReferences.Length; i++)
            {
                var reference = myReferences[i];
                myReferences[i] = reference.ReplaceBy(factory.CreateReferenceExpression("$0", name));
            }
            
            var firstReference = myReferences[0];
            if (declaration is ICSharpClosure closure && closure is ILambdaExpression lambda && lambda.BodyExpression != null)
            {

                if (myHasRead)
                {
                    var cacheStatement = factory.CreateStatement("var $0 = $1;", name, originValue.Copy());
                    var expression = firstReference.GetContainingNode<ILambdaExpression>()
                        .NotNull("Expression should be under lambda").BodyExpression;

                    var block = factory.CreateBlock("{$0return $1;}", cacheStatement, expression);
                    lambda.SetBodyBlock(block);
                }
            }
            else
            {
                if (myHasRead)
                {
                    var cacheStatement = factory.CreateStatement("var $0 = $1;", name, originValue.Copy());
                    var firstStatement = myReferences[0].GetContainingStatement();
                    StatementUtil.InsertStatement(cacheStatement, ref firstStatement, true);
                
                }

                if (myHasWrite)
                {
                    var restoreStatement = factory.CreateStatement("$0 = $1;", originValue, name);
                    var lastStatement = myReferences.Last().GetContainingStatement();
                    StatementUtil.InsertStatement(restoreStatement, ref lastStatement, false);
                }
            }

            return null;
        }
        
        private static bool IsAssignDestination(IReferenceExpression expr)
        {
            var fullReference = ReferenceExpressionNavigator.GetTopByQualifierExpression(expr);
            var assignment = AssignmentExpressionNavigator.GetByDest(fullReference.GetContainingParenthesizedExpression());
            return ExpressionStatementNavigator.GetByExpression(assignment) != null;
        }
        private static string GetUniqueName([NotNull]IReferenceExpression referenceExpression,[NotNull] string baseName)
        {
            var namingManager = referenceExpression.GetPsiServices().Naming;
            var policyProvider = namingManager.Policy.GetPolicyProvider(referenceExpression.Language, referenceExpression.GetSourceFile());
            var namingRule = policyProvider.GetPolicy(NamedElementKinds.Locals).NamingRule;
            var name = namingManager.Parsing.Parse(baseName, namingRule, policyProvider);
            var nameRoot = name.GetRootOrDefault(baseName);
            var namesCollection = namingManager.Suggestion.CreateEmptyCollection(PluralityKinds.Unknown, CSharpLanguage.Instance, true, policyProvider);
            namesCollection.Add(nameRoot, new EntryOptions(PluralityKinds.Unknown, SubrootPolicy.Decompose, emphasis: Emphasis.Good));
            var suggestionOptions = new SuggestionOptions
            {
                DefaultName = baseName,
                UniqueNameContext = referenceExpression,
            };
            var namesSuggestion = namesCollection.Prepare(NamedElementKinds.Locals, ScopeKind.Common, suggestionOptions);
            return namesSuggestion.FirstName();
        }
        
        public override string Text => "Cache property value";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return myReferences.All(t => t.IsValid());
        }
    }
}