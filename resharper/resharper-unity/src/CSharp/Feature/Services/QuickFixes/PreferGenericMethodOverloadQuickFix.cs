using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class PreferGenericMethodOverloadQuickFix : IQuickFix
    {
        private readonly IInvocationExpression myInvocationExpression;
        private readonly string myMethodName;
        private readonly ITypeElement myTypeElement;

        public PreferGenericMethodOverloadQuickFix(PreferGenericMethodOverloadWarning warning)
        {
            myInvocationExpression = warning.InvocationMethod;
            myMethodName = warning.MethodName;
            myTypeElement = warning.TypeElement;
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            return myTypeElement.IsValid() && myInvocationExpression.IsValid();
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            yield return new IntentionAction(
                new UseExplicitTypeInsteadOfStringAction(myTypeElement, myInvocationExpression, myMethodName), null,
                IntentionsAnchors.QuickFixesAnchor);
        }


        private class UseExplicitTypeInsteadOfStringAction : BulbActionBase
        {
            private readonly ITypeElement myType;
            private readonly IInvocationExpression myOldInvocation;
            private readonly string myMethodName;

            public UseExplicitTypeInsteadOfStringAction(ITypeElement type, IInvocationExpression oldInvocation, string methodName)
            {
                myType = type;
                myOldInvocation = oldInvocation;
                myMethodName = methodName;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution,
                IProgressIndicator progress)
            {
                var factory = CSharpElementFactory.GetInstance(myOldInvocation);
                if (myOldInvocation.InvokedExpression is IReferenceExpression referenceExpression)
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
                    builder.Argument(TypeFactory.CreateType(myType));
                    builder.Append(">()");

                    var newInvocation = factory.CreateExpression(builder.ToString(), builder.ToArguments());
                    myOldInvocation.ReplaceBy(newInvocation);
                }

                return null;
            }

            public override string Text => $"Convert to '{myMethodName}<{myType.GetClrName()}>()'";
        }
    }
}