using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class CachePropertyValueQuickFix : QuickFixBase
    {
        private readonly IReferenceExpression[] myReferences;
        private readonly bool myCanApplyFix;
        private readonly IReferenceExpression myHighlightedReference;

        public CachePropertyValueQuickFix(InefficientPropertyAccessWarning warning)
        {
            myReferences = warning.References;
            myCanApplyFix = warning.CanApplyFix;
            myHighlightedReference = warning.HighlightedReference;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var factory = CSharpElementFactory.GetInstance(myHighlightedReference);
            var propertyName = (myHighlightedReference.Reference.Resolve().DeclaredElement as IProperty).NotNull()
                .ShortName;

            var name = GetUniqueName(myHighlightedReference, propertyName);
            
            var loadDeclaration = factory.CreateStatement("var $0 = $1;", propertyName, myHighlightedReference.Copy());
            
            var firstAnchor = myReferences.First().GetContainingStatement();
            StatementUtil.InsertStatement(loadDeclaration, ref firstAnchor, true);
            
            var lastAnchor = myReferences.Last().GetContainingStatement();
            var saveDeclaration = factory.CreateStatement("$0 = $1;", myHighlightedReference.Copy(), propertyName);
            StatementUtil.InsertStatement(saveDeclaration, ref lastAnchor, false);
            
            foreach (var reference in myReferences)
            {
                reference.ReplaceBy(factory.CreateReferenceExpression("$0", propertyName));
            }
            
            return null;
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
            return myCanApplyFix && myReferences[0].IsValid();
        }
    }
}