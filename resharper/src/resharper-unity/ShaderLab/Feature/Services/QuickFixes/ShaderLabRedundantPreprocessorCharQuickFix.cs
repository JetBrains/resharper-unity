using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.Text;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.QuickFixes
{
    [QuickFix]
    public class ShaderLabRedundantPreprocessorCharQuickFix : IQuickFix
    {
        private readonly ITokenNode mySwallowedToken;

        public ShaderLabRedundantPreprocessorCharQuickFix(ShaderLabSwallowedPreprocessorCharWarning highlighting)
        {
            mySwallowedToken = highlighting.SwallowedToken;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            return new RemoveSwallowedToken(mySwallowedToken).ToQuickFixIntentions();
        }

        public bool IsAvailable(IUserDataHolder cache) => mySwallowedToken.IsValid();

        private class RemoveSwallowedToken : BulbActionBase
        {
            private readonly ITokenNode mySwallowedToken;

            public RemoveSwallowedToken(ITokenNode swallowedToken)
            {
                mySwallowedToken = swallowedToken;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                // TODO: When we have a code formatter for ShaderLab, we can just use CodeFormattingHelper.AddLineBreakAfter
                var lineEnding = mySwallowedToken
                    .GetContainingFile()
                    .DetectLineEnding(solution.GetPsiServices());

                var presentationAsBuffer = lineEnding.GetPresentationAsBuffer();
                return textControl =>
                {
                    textControl.Document.InsertText(mySwallowedToken.GetDocumentStartOffset().Offset, presentationAsBuffer.GetText());
                };
            }

            public override string Text => "Insert new line";
        }
    }
}