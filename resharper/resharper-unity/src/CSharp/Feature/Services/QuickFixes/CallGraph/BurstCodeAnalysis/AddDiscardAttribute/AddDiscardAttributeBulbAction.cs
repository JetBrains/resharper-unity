using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis.
    AddDiscardAttribute
{
    public sealed class AddDiscardAttributeBulbAction : IBulbAction
    {
        public AddDiscardAttributeBulbAction([NotNull] IMethodDeclaration methodDeclaration)
        {
            myMethodDeclaration = methodDeclaration;
        }

        [NotNull] private readonly IMethodDeclaration myMethodDeclaration;

        public void Execute(ISolution solution, ITextControl textControl)
        {
            AttributeUtil.AddAttributeToSingleDeclaration(myMethodDeclaration, KnownTypes.BurstDiscardAttribute,
                myMethodDeclaration.GetPsiModule(), CSharpElementFactory.GetInstance(myMethodDeclaration));
        }

        public string Text => AddDiscardAttributeUtil.DiscardActionMessage;
    }
}