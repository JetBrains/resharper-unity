using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Handlers;
using JetBrains.Application.UI.ActionSystem;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.InplaceRefactorings;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Refactorings.ChangeSignature;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class IncorrectMethodSignatureQuickFix : QuickFixBase
    {
        private readonly IMethodDeclaration myMethodDeclaration;
        private readonly MethodSignature myExpectedMethodSignature;
        private readonly MethodSignatureMatch myMatch;

        public IncorrectMethodSignatureQuickFix(InvalidStaticModifierWarning warning)
            : this(warning.MethodDeclaration, warning.ExpectedMethodSignature,
                MethodSignatureMatch.IncorrectStaticModifier)
        {
            var language = myMethodDeclaration.Language;
            var staticTerm = PresentationHelper.GetHelper(language).GetStaticTerm();

            if (myExpectedMethodSignature.IsStatic == true)
            {
                var declaredElement = myMethodDeclaration.DeclaredElement;
                Assertion.AssertNotNull(declaredElement, "declaredElement != null");
                var methodName = DeclaredElementPresenter.Format(language, DeclaredElementPresenter.NAME_PRESENTER,
                    declaredElement);
                Text = $"Make '{methodName}' {staticTerm}";
            }
            else
                Text = $"Remove '{staticTerm}' modifier";
        }

        public IncorrectMethodSignatureQuickFix(InvalidParametersWarning warning)
            : this(warning.MethodDeclaration, warning.ExpectedMethodSignature,
                MethodSignatureMatch.IncorrectParameters)
        {
            Text = $"Change parameters to '({warning.ExpectedMethodSignature.Parameters.GetParameterList()})'";
        }

        public IncorrectMethodSignatureQuickFix(InvalidReturnTypeWarning warning)
            : this(warning.MethodDeclaration, warning.ExpectedMethodSignature, MethodSignatureMatch.IncorrectReturnType)
        {
            Text = $"Change return type to '{warning.ExpectedMethodSignature.GetReturnTypeName()}'";
        }

        public IncorrectMethodSignatureQuickFix(InvalidTypeParametersWarning warning)
            : this(warning.MethodDeclaration, warning.ExpectedMethodSignature,
                MethodSignatureMatch.IncorrectTypeParameters)
        {
            Text = "Remove type parameters";
        }

        public IncorrectMethodSignatureQuickFix(IncorrectSignatureWarning warning)
            : this(warning.MethodDeclaration, warning.ExpectedMethodSignature, warning.MethodSignatureMatch)
        {
            Text = $"Change signature to '{warning.ExpectedMethodSignature.FormatSignature(warning.MethodDeclaration.DeclaredName)}'";
        }

        private IncorrectMethodSignatureQuickFix(IMethodDeclaration methodDeclaration,
            MethodSignature expectedMethodSignature, MethodSignatureMatch match)
        {
            myMethodDeclaration = methodDeclaration;
            myExpectedMethodSignature = expectedMethodSignature;
            myMatch = match;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            Action<ITextControl> action = null;

            if ((myMatch & MethodSignatureMatch.IncorrectStaticModifier) ==
                MethodSignatureMatch.IncorrectStaticModifier && myExpectedMethodSignature.IsStatic.HasValue)
            {
                myMethodDeclaration.SetStatic(myExpectedMethodSignature.IsStatic.Value);
            }

            if ((myMatch & MethodSignatureMatch.IncorrectParameters) == MethodSignatureMatch.IncorrectParameters)
                action = ChangeParameters(solution);

            if ((myMatch & MethodSignatureMatch.IncorrectReturnType) == MethodSignatureMatch.IncorrectReturnType)
            {
                var element = myMethodDeclaration.DeclaredElement;
                Assertion.AssertNotNull(element, "element != null");

                var language = myMethodDeclaration.Language;
                var changeTypeHelper = LanguageManager.Instance.GetService<IChangeTypeHelper>(language);
                changeTypeHelper.ChangeType(myExpectedMethodSignature.ReturnType, element);
            }

            if ((myMatch & MethodSignatureMatch.IncorrectTypeParameters) ==
                MethodSignatureMatch.IncorrectTypeParameters)
            {
                // There are no generic Unity methods, so just remove any that are already there
                myMethodDeclaration.SetTypeParameterList(null);
            }

            return action;
        }

        public override string Text { get; }

        public override bool IsAvailable(IUserDataHolder cache) => ValidUtils.Valid(myMethodDeclaration);

        private Action<ITextControl> ChangeParameters(ISolution solution)
        {
            var changeSignature = LanguageManager.Instance.TryGetService<ChangeSignature>(myMethodDeclaration.Language);
            if (changeSignature == null)
                return null;

            var model = changeSignature.CreateModel(myMethodDeclaration.DeclaredElement);
            for (var i = 0; i < myExpectedMethodSignature.Parameters.Length; i++)
            {
                var requiredParameter = myExpectedMethodSignature.Parameters[i];

                var modelParameter = FindBestMatch(requiredParameter, model, i);
                if (modelParameter != null)
                {
                    model.MoveTo(modelParameter.OriginalParameterIndex, i);
                }
                else
                {
                    model.Add(i);
                    modelParameter = model.ChangeSignatureParameters[i];
                }

                modelParameter.ParameterName = requiredParameter.Name;
                modelParameter.ParameterKind = ParameterKind.VALUE;
                modelParameter.ParameterType = requiredParameter.Type;

                // Reset everything else
                modelParameter.DefaultValue = null;
                modelParameter.IsOptional = false;
                modelParameter.IsParams = false;
                modelParameter.IsThis = false;
                modelParameter.IsVarArg = false;
            }

            for (var i = model.ChangeSignatureParameters.Length - 1; i >= myExpectedMethodSignature.Parameters.Length; i--)
            {
                model.Remove(i);
            }

            var refactoring = new ChangeSignatureRefactoring(model);
            refactoring.Execute(NullProgressIndicator.Create());

            // Ideally, we would now call InplaceRefactoringsManager.Reset to make sure we didn't have
            // an inplace refactoring highlight. But InplaceRefactoringsManager is internal, so we can't.
            // We don't want a highlight telling us to "apply signature change refactoring" because we
            // just have. The only way to remove it is to fire the Escape action
            return tc =>
            {
                var highlightingManager = solution.GetComponent<InplaceRefactoringsHighlightingManager>();
                if (highlightingManager.GetHighlightersForTests(tc).Any())
                {
                    var actionManager = solution.GetComponent<IActionManager>();
                    var escapeActionHandler = actionManager.Defs.GetActionDef<EscapeActionHandler>();
                    escapeActionHandler.EvaluateAndExecute(actionManager);
                }
            };
        }

        private ChangeSignatureParameter FindBestMatch(ParameterSignature requiredParameter, ChangeSignatureModel model, int i)
        {
            // Try and match type and name first
            for (var j = i; j < model.ChangeSignatureParameters.Length; j++)
            {
                if (model.ChangeSignatureParameters[j].ParameterName == requiredParameter.Name
                    && Equals(model.ChangeSignatureParameters[j].ParameterType, requiredParameter.Type))
                {
                    return model.ChangeSignatureParameters[j];
                }
            }

            // Now just match type - we'll update name after
            for (var j = i; j < model.ChangeSignatureParameters.Length; j++)
            {
                if (Equals(model.ChangeSignatureParameters[j].ParameterType, requiredParameter.Type))
                {
                    return model.ChangeSignatureParameters[j];
                }
            }

            return null;
        }
    }
}