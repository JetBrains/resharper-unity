using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes
{
    [QuickFix]
    public class InvalidStaticModifierQuickFix : QuickFixBase
    {
        private readonly UnityEventFunction myEventFunction;
        private readonly IMethodDeclaration myMethodDeclaration;

        public InvalidStaticModifierQuickFix(InvalidStaticModifierWarning warning)
        {
            myEventFunction = warning.Function;
            myMethodDeclaration = warning.MethodDeclaration;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            myMethodDeclaration.SetStatic(myEventFunction.IsStatic);
            return null;
        }

        public override string Text
        {
            get
            {
                var language = myMethodDeclaration.Language;
                var staticTerm = PresentationHelper.GetHelper(language).GetStaticTerm();

                if (myEventFunction.IsStatic)
                {
                    var methodName = DeclaredElementPresenter.Format(language, DeclaredElementPresenter.NAME_PRESENTER,
                        myMethodDeclaration.DeclaredElement);
                    return
                        $"Make '{methodName}' {staticTerm}";
                }
                return $"Remove '{staticTerm}' modifier";
            }
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return ValidUtils.Valid(myMethodDeclaration);
        }
    }
}