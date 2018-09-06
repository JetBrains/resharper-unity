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
    public class UseExplicitTypeInsteadOfStringQuickFix : IQuickFix
    {
        [NotNull] public static readonly InvisibleAnchor CreateFromUsageAnchor =
            new InvisibleAnchor(ResolveProblemsFixAnchors.CreateFromUsageAnchor);

        private readonly IInvocationExpression myInvocationExpression;
        private readonly string myMethodName;
        private readonly string myStringLiteral;
        private readonly ITypeElement[] myAvailableTypes;

        public UseExplicitTypeInsteadOfStringQuickFix(UseExplicitTypeInsteadOfStringUsingWarning warning)
        {
            myInvocationExpression = warning.InvocationMethod;
            myMethodName = warning.MethodName;
            myStringLiteral = warning.StringLiteral;
            myAvailableTypes = warning.AvailableTypes;
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            return myInvocationExpression.IsValid();
        }


        private IReadOnlyList<IBulbAction> GetItems()
        {
            return myAvailableTypes.Select(t =>
                    new UseExplicitTypeInsteadOfStringAction(t, myInvocationExpression, myMethodName, myStringLiteral))
                .ToList();
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            foreach (var action in GetItems())
            {
                var anchor = new SubmenuAnchor(CreateFromUsageAnchor, SubmenuBehavior.Executable);
                yield return new IntentionAction(action, null, anchor);
            }
        }


        private class UseExplicitTypeInsteadOfStringAction : BulbActionBase
        {
            private readonly ITypeElement myType;
            private readonly IInvocationExpression myOldInvocation;
            private readonly string myMethodName;
            private readonly string myLiteralName;

            public UseExplicitTypeInsteadOfStringAction(ITypeElement type, IInvocationExpression oldInvocation,
                string methodName, string literalName)
            {
                myType = type;
                myOldInvocation = oldInvocation;
                myMethodName = methodName;
                myLiteralName = literalName;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution,
                IProgressIndicator progress)
            {
                using (WriteLockCookie.Create())
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
                        builder.Append(myType.GetClrName().FullName);
                        builder.Append(">");
                        builder.Append("()");

                        var newInvocation = factory.CreateExpression(builder.ToString(), builder.ToArguments());
                        var result = ModificationUtil.ReplaceChild(myOldInvocation, newInvocation);

                        // beatify generated reference
                        var resultTextRange = result.GetDocumentRange();

                        var psiServices = solution.GetPsiServices();
                        if (psiServices.GetPsiFile<CSharpLanguage>(resultTextRange) is ICSharpFile file)
                        {
                            file.OptimizeImportsAndRefs(
                                resultTextRange.CreateRangeMarker(DocumentManager.GetInstance(solution)), false, true,
                                NullProgressIndicator.Instance);
                        }
                    }
                }

                return null;
            }

            public override string Text =>
                $"Change .{myMethodName}(\"{myLiteralName}\") to .{myMethodName}<{myType.GetClrName()}>()";
        }
    }
}