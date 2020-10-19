using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Comment;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph
{
    public abstract class AddCommentActionBase : IBulbAction
    {
        [NotNull] private readonly IMethodDeclaration myMethodDeclaration;
        protected abstract string Comment { get; }
        public abstract string Text { get; }

        protected AddCommentActionBase([NotNull] IMethodDeclaration methodDeclaration)
        {
            myMethodDeclaration = methodDeclaration;
        }
        
        public void Execute(ISolution solution, ITextControl textControl)
        {
            var file = myMethodDeclaration.GetContainingFile();
            
            if (file == null) 
                return;
            
            var treeRange = myMethodDeclaration.GetTreeTextRange();
            var provider = LanguageManager.Instance.GetService<ICommentOrDirectiveInserter>(file.Language);

            provider.Insert(treeRange, file, Text, Comment);
        }
    }
}