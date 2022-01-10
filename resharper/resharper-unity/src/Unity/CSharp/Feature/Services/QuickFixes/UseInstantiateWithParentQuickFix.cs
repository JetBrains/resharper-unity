using System;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class UseInstantiateWithParentQuickFix : UnityScopedQuickFixBase
    {
        private readonly IInvocationExpression myInvocation;
        private readonly ICSharpExpression myNewArgument;
        private readonly bool myStayInWorldCoords;

        public UseInstantiateWithParentQuickFix(InstantiateWithoutParentWarning warning)
        {
            myInvocation = warning.Invocation;
            myNewArgument = warning.NewArgument;
            myStayInWorldCoords = warning.StayInWorldCoords;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var statement = myNewArgument.GetContainingStatement().NotNull("myNewArgument.GetContainingStatement() != null");

            var factory = CSharpElementFactory.GetInstance(myInvocation);

            var first = myInvocation.Arguments[0];
            var second = myInvocation.AddArgumentAfter(factory.CreateArgument(ParameterKind.VALUE, myNewArgument.Copy()), first);
            var boolLiteral = factory.CreateExpression(myStayInWorldCoords.ToString().ToLower());
            myInvocation.AddArgumentAfter(factory.CreateArgument(ParameterKind.VALUE, boolLiteral), second);

            statement.RemoveOrReplaceByEmptyStatement();

            return null;
        }

        public override string Text => "Combine with object creation";

        public override bool IsAvailable(IUserDataHolder cache) =>
            myNewArgument != null && base.IsAvailable(cache) && ValidUtils.Valid(myNewArgument);

        protected override ITreeNode TryGetContextTreeNode() => myInvocation;
    }
}