using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class TypeInsteadOfStringQuickFix : QuickFixBase
    {
        private readonly IInvocationExpression myInvocationExpression;
        private readonly string myMethodName;
        private readonly string myTypeName;

        public TypeInsteadOfStringQuickFix(TypeInsteadOfStringUsingWarning warning)
        {
            myInvocationExpression = warning.InvocationMethod;
            myMethodName = warning.MethodName;
            myTypeName = warning.TypeLiteral;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            { 
                var factory = CSharpElementFactory.GetInstance(myInvocationExpression);
                var newExpression = factory.CreateExpression($"{myMethodName}<{myTypeName}>()"); 
                ModificationUtil.ReplaceChild(myInvocationExpression, newExpression);
            }
            return null;
        }

        public override string Text => "Use generic overload instead of string";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return ValidUtils.Valid(myInvocationExpression);
        }
    }
}