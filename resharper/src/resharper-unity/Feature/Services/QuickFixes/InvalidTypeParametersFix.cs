using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes
{
    [QuickFix]
    public class InvalidTypeParametersFix : QuickFixBase
    {
        private readonly IMethodDeclaration myMethodDeclaration;

        public InvalidTypeParametersFix(InvalidTypeParametersWarning warning)
        {
            myMethodDeclaration = warning.MethodDeclaration;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            // None of our methods are generic, so the only fix we can do is to remove type parameters
            myMethodDeclaration.SetTypeParameterList(null);
            return null;
        }

        public override string Text => "Remove type parameters";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return ValidUtils.Valid(myMethodDeclaration);
        }
    }
}