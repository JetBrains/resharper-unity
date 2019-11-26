using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class  MultiplicationOrderQuickFix : QuickFixBase
    {
        private readonly ICSharpExpression myExpression;
        private List<ICSharpExpression> myScalars;
        private List<ICSharpExpression> myMatrices;

        public MultiplicationOrderQuickFix(InefficientMultiplicationOrderWarning warning)
        {
            myExpression = warning.Expression;
            myScalars = warning.Scalars;
            myMatrices = warning.Matrices;
        }
        
        private const string mul = "$0 * $1";

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var factory = CSharpElementFactory.GetInstance(myExpression);

            var matrices = CreateMulExpression(factory, myMatrices);
            var scalars = CreateMulExpression(factory, myScalars);
            myExpression.ReplaceBy(factory.CreateExpression(mul, matrices, scalars));
            
            return null;
        }

        private ICSharpExpression CreateMulExpression(CSharpElementFactory factory, List<ICSharpExpression> elements)
        {
            if (elements.Count == 1)
                return elements[0].CopyWithResolve();
            
            var newExpr = factory.CreateExpression(mul, elements[0].CopyWithResolve(), elements[1].CopyWithResolve());
            for (int i = 2; i < elements.Count; i++)
                newExpr = factory.CreateExpression(mul, newExpr, elements[i].CopyWithResolve());

            return newExpr;
        }
        
        public override bool IsAvailable(IUserDataHolder cache) => myExpression.IsValid();

        public override string Text => "Reorder multiplication";

    }
}