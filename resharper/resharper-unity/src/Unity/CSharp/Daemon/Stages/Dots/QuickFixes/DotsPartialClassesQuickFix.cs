using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots.QuickFixes
{
    [QuickFix]
    public class DotsPartialClassesQuickFix : UnityScopedQuickFixBase
    {
        private readonly InheritorMustBeMarkedPartialReadonlyWarning myInheritorMustBeMarkedPartialReadonlyWarning;
        private readonly IClassLikeDeclaration myClassLikeDeclaration;
        
        public DotsPartialClassesQuickFix(InheritorMustBeMarkedPartialReadonlyWarning inheritorMustBeMarkedPartialReadonlyWarning)
        {
            myInheritorMustBeMarkedPartialReadonlyWarning = inheritorMustBeMarkedPartialReadonlyWarning;
            myClassLikeDeclaration = inheritorMustBeMarkedPartialReadonlyWarning.ClassLikeDeclaration;
        }

        private string GetText()
        {
            var mustBeReadonly = myInheritorMustBeMarkedPartialReadonlyWarning.MustBeReadonly;
            var mustBePartial = myInheritorMustBeMarkedPartialReadonlyWarning.MustBePartial;
            var modifiersString = $"{(mustBePartial ? "partial" : string.Empty)}{(mustBePartial && mustBeReadonly ? " " : string.Empty)}{(mustBeReadonly ? "readonly" : string.Empty)}";
            return string.Format(Strings.UnityDots_DotsPartialClassesQuickFix_Add_Partial_Readonly, modifiersString, myClassLikeDeclaration.DeclaredName);
        }

        public override string Text => GetText();
        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            {
                if (myInheritorMustBeMarkedPartialReadonlyWarning.MustBeReadonly)
                    myClassLikeDeclaration.SetReadonly(true);
                if (myInheritorMustBeMarkedPartialReadonlyWarning.MustBePartial)
                    myClassLikeDeclaration.SetPartial(true);
            }

            return null;
        }

        protected override ITreeNode TryGetContextTreeNode()
        {
            return myClassLikeDeclaration;
        }
    }
}