using System;
using JetBrains.Application.Progress;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros.Implementations;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
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
    public class ConvertToGameObjectAddComponentQuickFix : QuickFixBase
    {
        private readonly IObjectCreationExpression myWarningCreationExpression;

        public ConvertToGameObjectAddComponentQuickFix(IncorrectMonoBehaviourInstantiationWarning warning)
        {
            myWarningCreationExpression = warning.CreationExpression;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            IInvocationExpression newExpression;
            using (WriteLockCookie.Create())
            {
                var factory = CSharpElementFactory.GetInstance(myWarningCreationExpression);
                newExpression = (IInvocationExpression) factory.CreateExpression("gameObject.AddComponent<$0>()", myWarningCreationExpression.ExplicitType());
                newExpression = ModificationUtil.ReplaceChild(myWarningCreationExpression, newExpression);
            }

            return textControl =>
            {
                var qualifier = newExpression.ExtensionQualifier;
                Assertion.AssertNotNull(qualifier, "qualifier != null");
                var hotspotExpression = new MacroCallExpressionNew(new SuggestVariableOfTypeMacroDef());
                hotspotExpression.AddParameter(new ConstantMacroParameter("UnityEngine.GameObject"));
                var field = new TemplateField("gameObject", hotspotExpression, 0);
                HotspotInfo[] fieldInfos = {
                    new HotspotInfo(field, qualifier.GetDocumentRange())
                };

                var manager = LiveTemplatesManager.Instance;
                var invalidRange = DocumentRange.InvalidRange;

                var session = manager.CreateHotspotSessionAtopExistingText(solution, invalidRange,
                    textControl, LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, fieldInfos);
                session.Execute();
            };
        }

        public override string Text =>
            $"Convert to 'GameObject.AddComponent<{myWarningCreationExpression.TypeName.ShortName}>()'";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return ValidUtils.Valid(myWarningCreationExpression);
        }
    }
}