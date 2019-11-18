using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class  MultiplicationOrderQuickFix : QuickFixBase
    {
        private readonly ICSharpExpression myExpression;
        private readonly ICSharpExpression myMatrix;
        private readonly ICSharpExpression myScalar;

        public MultiplicationOrderQuickFix(InefficientMultiplicationOrderWarning warning)
        {
            myExpression = warning.Expression;
            myMatrix = warning.OperandMatrix;
            myScalar = warning.OperandScalar;
        }
        
        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var newMatrix = myMatrix.CopyWithResolve();
            var newScalar = myScalar.CopyWithResolve();
            
            myMatrix.ReplaceBy(newScalar);
            myScalar.ReplaceBy(newMatrix);
            return null;
        }

        public override bool IsAvailable(IUserDataHolder cache) => myExpression.IsValid();

        public override string Text => "Reorder multiplication";

    }
}