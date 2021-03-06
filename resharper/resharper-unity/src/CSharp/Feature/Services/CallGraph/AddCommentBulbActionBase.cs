using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Comment;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public abstract class AddCommentBulbActionBase : IBulbAction
    {
        [NotNull] private readonly IMethodDeclaration myMethodDeclaration;
        
        protected AddCommentBulbActionBase([NotNull] IMethodDeclaration methodDeclaration)
        {
            myMethodDeclaration = methodDeclaration;
        }
        
        public abstract string Text { get; }
        [NotNull] protected abstract string Comment { get; }
        
        public void Execute(ISolution solution, ITextControl textControl)
        {
            solution.Locks.AssertReadAccessAllowed();
            var file = myMethodDeclaration.GetContainingFile();
            
            if (file == null) 
                return;
            
            var treeRange = myMethodDeclaration.GetTreeTextRange();
            var inserter = LanguageManager.Instance.GetService<ICommentOrDirectiveInserter>(file.Language);

            inserter.Insert(treeRange, file, Text, Comment);
        }
    }
}