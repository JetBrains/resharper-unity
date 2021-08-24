using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Feature.Services.QuickFixes
{
    [QuickFix]
    public class AsmDefRemoveInvalidArrayItemQuickFix : QuickFixBase
    {
        private readonly bool myIsValid = true;
        [CanBeNull] private readonly IJsonNewLiteralExpression myLiteral;

        // TODO: Copied from R# JSON based implementation
        // public AsmDefRemoveInvalidArrayItemQuickFix(JsonValidationFailedWarning warning)
        // {
            // myLiteral = warning.Brace as IJavaScriptLiteralExpression;
            // myIsValid = warning.AssertionResult.Description ==
                                        // AsmDefDuplicateItemsProblemAnalyzer.AsmDefDuplicateItemDescription;
        // }

        public AsmDefRemoveInvalidArrayItemQuickFix(ReferencingSelfError error)
        {
            myLiteral = error.Reference.GetTreeNode() as IJsonNewLiteralExpression;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var arrayLiteral = JsonNewArrayNavigator.GetByValue(myLiteral);
            arrayLiteral?.RemoveArrayElement(myLiteral);
            return null;
        }

        public override string Text => "Remove invalid value";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return myIsValid && ValidUtils.Valid(myLiteral);
        }
    }
}