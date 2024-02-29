using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class ConvertCoalescingToConditionalQuickFix(UnityObjectNullCoalescingWarning warning) : UnityScopedQuickFixBase
    {
        private readonly ITreeNode myOperator = warning.Node;

        public override bool IsAvailable(IUserDataHolder cache) => myOperator.Parent is INullCoalescingExpression or IAssignmentExpression && base.IsAvailable(cache);

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            {
                var factory = CSharpElementFactory.GetInstance(myOperator);
                var parent = myOperator.Parent;
                var newExpression = parent switch
                {
                    INullCoalescingExpression coalescing => factory.CreateExpression("$0?$0:$1", coalescing.LeftOperand, coalescing.RightOperand),
                    IAssignmentExpression assignment => factory.CreateExpression("$0=$0?$0:$1", assignment.Dest, assignment.Source),
                    _ => throw new NotSupportedException($"Unexpected expression type for ConvertCoalescingToConditionalQuickFix: {parent?.GetType()}")
                };
                ModificationUtil.ReplaceChild(parent, newExpression);
            }
            return null;
        }

        protected override ITreeNode TryGetContextTreeNode() => myOperator;
        public override string Text => Strings.ConvertCoalescingToConditionalQuickFix_Text_Convert_to_conditional_expression;
    }
}
