using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using Lex;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.QuickFixes
{
    [QuickFix]
    public class InvalidParametersOnVariableReferenceQuickFix : IQuickFix
    {
        private readonly IInvalidVariableReferenceParameters myInvalidVariableReferenceParameters;

        public InvalidParametersOnVariableReferenceQuickFix(ShaderLabInvalidVariableReferenceParametersWarning highlighting)
        {
            myInvalidVariableReferenceParameters = highlighting.InvalidParameters;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            return new RemoveToken(myInvalidVariableReferenceParameters).ToQuickFixIntentions();
        }

        public bool IsAvailable(IUserDataHolder cache) => myInvalidVariableReferenceParameters.IsValid();

        private class RemoveToken : BulbActionBase
        {
            private readonly IInvalidVariableReferenceParameters myToken;

            public RemoveToken(IInvalidVariableReferenceParameters token)
            {
                myToken = token;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                using (WriteLockCookie.Create())
                {
                    var reference = VariableReferenceNavigator.GetByInvalidParameters(myToken);
                    ModificationUtil.DeleteChild(myToken);
                    // TODO: Remove this when we finally get a formatter
                    var firstWhitespace = reference?.Name?.NextSibling;
                    var lastWhitespace = reference?.RBrack?.PrevSibling;
                    if (firstWhitespace.IsWhitespaceToken() && lastWhitespace.IsWhitespaceToken())
                        ModificationUtil.DeleteChildRange(firstWhitespace, lastWhitespace);
                }
                return null;
            }

            public override string Text => "Remove invalid parameters";
        }
    }
}