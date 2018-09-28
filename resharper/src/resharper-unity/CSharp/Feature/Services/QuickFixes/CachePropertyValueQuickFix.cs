using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LinqTools;
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
        private readonly bool myInlineCache;
        private readonly bool myInlineRestore;
        private ICSharpStatement myCacheAnchor;
        private ICSharpStatement myRestoreAnchor;
        
        public CachePropertyValueQuickFix(InefficientPropertyAccessWarning warning)
        {
            myReferences = warning.References;
            myHighlightedReference = warning.HighlightedReference;
            myInlineCache = warning.InlineCacheValue;
            myInlineRestore = warning.InlineRestoreValue;
            myCacheAnchor = warning.CacheAnchor;
            myRestoreAnchor = warning.RestoreAnchor;
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
                var cacheStatement = factory.CreateStatement("$0 $1;",type, name);
                
                // inline caching
                var cacheExpression = firstReference.ReplaceBy(factory.CreateExpression("($0 = $1)", name, originValue.Copy()));
                var expression = cacheExpression.GetContainingNode<ILambdaExpression>()
                    .NotNull("Expression should be under lambda").BodyExpression;

                var block = factory.CreateBlock("{$0return $1;}", cacheStatement, expression);
                lambda.SetBodyBlock(block);
            }
            else
            {
                if (myInlineCache) // replace first read with assignment expression
                {
                    foreach (var reference in myReferences)
                    {
                        if (reference.GetContainingStatement() != myCacheAnchor) 
                            continue;
                        
                        // is write first???
                        // example: var x = (transform.position = Vector3.Up) + transform.position + transform.position ...
                        // if yes, we have already save our variable in cycle above, if no use inline to cache.
                        if (AssignmentExpressionNavigator.GetByDest(reference.GetContainingParenthesizedExpression()) == null)
                        {
                            reference.ReplaceBy(factory.CreateExpression("($0 = $1)", name, originValue.Copy()));
                        }
                        break;
                    }

                    var cacheStatement = factory.CreateStatement("$0 $1;", type ,name);    
                    StatementUtil.InsertStatement(cacheStatement, ref myCacheAnchor, true);
                }
                else
                {
                    var cacheStatement = factory.CreateStatement("var $0 = $1;", name, originValue.Copy());    
                    StatementUtil.InsertStatement(cacheStatement, ref myCacheAnchor, true);
                }
                
                if (myRestoreAnchor != null)
                {
                    if (myInlineRestore)
                    {
                        foreach (var reference in myReferences.Reverse())
                        {
                            if (reference.GetContainingStatement() == myRestoreAnchor)
                            {
                                reference.ReplaceBy(factory.CreateReferenceExpression("$0", originValue));
                                break;
                            }
                        }
                    }
                    else
                    {
                        var restoreStatement = factory.CreateStatement("$0 = $1;", originValue, name);
                        StatementUtil.InsertStatement(restoreStatement, ref myRestoreAnchor, false);
                    }
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