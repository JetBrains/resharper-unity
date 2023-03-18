using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots.QuickFixes
{
    [QuickFix]
    public class MustBeStructForDotsInheritorQuickFix : UnityScopedQuickFixBase
    {
        private readonly MustBeStructForDotsInheritorWarning myMustBeStructForDotsInheritorWarning;
        private readonly IClassLikeDeclaration myClassLikeDeclaration;

        public MustBeStructForDotsInheritorQuickFix(MustBeStructForDotsInheritorWarning mustBeStructForDotsInheritorWarning)
        {
            myMustBeStructForDotsInheritorWarning = mustBeStructForDotsInheritorWarning;
            myClassLikeDeclaration = mustBeStructForDotsInheritorWarning.ClassLikeDeclaration;
        }

        public override string Text => myMustBeStructForDotsInheritorWarning.ToolTip!;
        
        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            {
                LowLevelModificationUtil.ReplaceChild(myClassLikeDeclaration.TypeDeclarationKeyword,
                    CSharpTokenType.STRUCT_KEYWORD.CreateLeafElement());
                var  targetNodeType = ElementType.STRUCT_DECLARATION;
                
                var targetPartDeclaration = targetNodeType.CreateTreeElement();
                SandBox.CreateSandBoxWithContextFor(
                    targetPartDeclaration, myClassLikeDeclaration.GetPsiModule(),
                    context: myClassLikeDeclaration.Parent, SandBoxContextType.Child,
                    myClassLikeDeclaration.Language);

                LowLevelModificationUtil.AddChild(targetPartDeclaration, myClassLikeDeclaration.Children().ToArray());
                LowLevelModificationUtil.ReplaceChild(myClassLikeDeclaration, targetPartDeclaration);
            }

            return null;
        }

        protected override ITreeNode TryGetContextTreeNode()
        {
            return myClassLikeDeclaration;
        }
    }
}