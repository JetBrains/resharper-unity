using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.DocumentManagers;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.CreateFromUsage;
using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
using JetBrains.ReSharper.Intentions.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class PreferGenericMethodOverloadQuickFix : IQuickFix
    {
        [NotNull] public static readonly InvisibleAnchor IntentionAnchor =
            new InvisibleAnchor(IntentionsAnchors.QuickFixesAnchor);

        private readonly IInvocationExpression myInvocationExpression;
        private readonly string myMethodName;
        private readonly ITypeElement[] myAvailableTypes;

        public PreferGenericMethodOverloadQuickFix(PreferGenericMethodOverloadWarning warning)
        {
            myInvocationExpression = warning.InvocationMethod;
            myMethodName = warning.MethodName;
            myAvailableTypes = warning.AvailableTypes;
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            return myAvailableTypes.Length > 0 && myInvocationExpression.IsValid();
        }


        private IReadOnlyList<IBulbAction> GetItems()
        {
            return myAvailableTypes.Select(t =>
                    new UseExplicitTypeInsteadOfStringAction(t, myInvocationExpression, myMethodName))
                .ToList();
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            foreach (var action in GetItems())
            {
                var anchor = new SubmenuAnchor(IntentionAnchor, SubmenuBehavior.Executable);
                yield return new IntentionAction(action, null, anchor);
            }
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
                    builder.Append(">");
                    builder.Append("()");

                    var newInvocation = factory.CreateExpression(builder.ToString(), builder.ToArguments());
                    myOldInvocation.ReplaceBy(newInvocation);

                }

                return null;
            }

            public override string Text => $"Convert to '{myMethodName}<{myType.GetClrName()}>()'";
        }
    }
}