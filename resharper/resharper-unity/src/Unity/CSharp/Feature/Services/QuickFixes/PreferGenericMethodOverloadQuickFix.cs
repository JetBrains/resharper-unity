using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class PreferGenericMethodOverloadQuickFix : UnityScopedQuickFixBase
    {
        private readonly IInvocationExpression myInvocationExpression;
        private readonly string myMethodName;
        private readonly ITypeElement myTargetType;

        public PreferGenericMethodOverloadQuickFix(PreferGenericMethodOverloadWarning warning)
        {
            myInvocationExpression = warning.InvocationMethod;
            myMethodName = warning.MethodName;
            myTargetType = warning.TypeElement;
        }

        public override bool IsAvailable(IUserDataHolder cache) => base.IsAvailable(cache) && myTargetType.IsValid();
        public override string Text => string.Format(Strings.PreferGenericMethodOverloadQuickFix_Text_Convert_to__MethodName__1__, myMethodName, myTargetType.GetClrName());

        // Can't use method name or target type for scoped fixes. This text is weak, but it's a sub-menu under something
        // like "Convert to 'GetComponent<UnityEngine.Grid>()'", so I think it gets the point across. I don't think
        // people will be too surprised that this will change other methods, too.
        public override string ScopedText => Strings.PreferGenericMethodOverloadQuickFix_ScopedText_Use_strongly_typed_overloads;
        protected override ITreeNode TryGetContextTreeNode() => myInvocationExpression;

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var factory = CSharpElementFactory.GetInstance(myInvocationExpression);
            if (myInvocationExpression.InvokedExpression is IReferenceExpression referenceExpression)
            {
                var builder = FactoryArgumentsBuilder.Create();

                var qualifier = referenceExpression.QualifierExpression;
                if (qualifier != null)
                {
                    builder.Argument(qualifier);
                    builder.Append(".");
                }

                builder.Append(myMethodName);
                builder.Append("<");
                builder.Argument(TypeFactory.CreateType(myTargetType));
                builder.Append(">()");

                var newInvocation = factory.CreateExpression(builder.ToString(), builder.ToArguments());
                myInvocationExpression.ReplaceBy(newInvocation);
            }

            return null;
        }
    }
}