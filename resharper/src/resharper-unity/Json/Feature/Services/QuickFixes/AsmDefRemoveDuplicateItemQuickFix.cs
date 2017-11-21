using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.JavaScript.Stages;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.QuickFixes
{
    [QuickFix]
    public class AsmDefRemoveDuplicateItemQuickFix : QuickFixBase
    {
        private readonly bool myIsDuplicateValueWarning;
        [CanBeNull] private readonly IJavaScriptLiteralExpression myLiteral;

        public AsmDefRemoveDuplicateItemQuickFix(JsonValidationFailedWarning warning)
        {
            myLiteral = warning.Brace as IJavaScriptLiteralExpression;
            myIsDuplicateValueWarning = warning.AssertionResult.Description ==
                                        AsmDefDuplicateItemsProblemAnalyzer.AsmDefDuplicateItemDescription;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var arrayLiteral = ArrayLiteralNavigator.GetByArrayElement(myLiteral);
            arrayLiteral?.RemoveArrayElement(myLiteral);
            return null;
        }

        public override string Text => "Remove invalid value";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return myIsDuplicateValueWarning && ValidUtils.Valid(myLiteral);
        }
    }
}