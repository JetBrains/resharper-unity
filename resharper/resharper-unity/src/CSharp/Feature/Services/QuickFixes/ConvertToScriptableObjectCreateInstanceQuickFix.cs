using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class ConvertToScriptableObjectCreateInstanceQuickFix : QuickFixBase
    {
        private readonly IObjectCreationExpression myWarningCreationExpression;

        public ConvertToScriptableObjectCreateInstanceQuickFix(IncorrectScriptableObjectInstantiationWarning warning)
        {
            myWarningCreationExpression = warning.CreationExpression;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            {
                var scriptableObjectType =
                    TypeFactory.CreateTypeByCLRName(KnownTypes.ScriptableObject, myWarningCreationExpression.GetPsiModule());

                var factory = CSharpElementFactory.GetInstance(myWarningCreationExpression);
                var newExpression = factory.CreateExpression("$0.CreateInstance<$1>()",
                    scriptableObjectType, myWarningCreationExpression.ExplicitType());
                ModificationUtil.ReplaceChild(myWarningCreationExpression, newExpression);
            }

            return null;
        }

        public override string Text =>
            $"Convert to 'ScriptableObject.CreateInstance<{myWarningCreationExpression.TypeName.ShortName}>()'";

        public override bool IsAvailable(IUserDataHolder cache) => ValidUtils.Valid(myWarningCreationExpression);
    }
}