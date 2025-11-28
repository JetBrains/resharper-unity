using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.BulbActions;
using JetBrains.ReSharper.Feature.Services.Comment;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public abstract class AddCommentBulbActionBase : ModernBulbActionBase
    {
        [NotNull] private readonly IMethodDeclaration myMethodDeclaration;

        protected AddCommentBulbActionBase([NotNull] IMethodDeclaration methodDeclaration)
        {
            myMethodDeclaration = methodDeclaration;
        }

        [NotNull] protected abstract string Comment { get; }

        protected override IBulbActionCommand ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            solution.Locks.AssertReadAccessAllowed();

            var file = myMethodDeclaration.GetContainingFile();
            if (file == null) return null;

            var treeRange = myMethodDeclaration.GetTreeTextRange();
            var inserter = LanguageManager.Instance.GetService<ICommentOrDirectiveInserter>(file.Language);

            return inserter.Insert(treeRange, file, Comment);
        }
    }
}