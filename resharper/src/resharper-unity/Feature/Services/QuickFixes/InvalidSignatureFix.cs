using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Handlers;
using JetBrains.Application.UI.ActionSystem;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.InplaceRefactorings;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Refactorings.ChangeSignature;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes
{
    [QuickFix]
    public class InvalidSignatureFix : QuickFixBase
    {
        private readonly IMethodDeclaration myMethodDeclaration;
        private readonly MethodSignature myMethodSignature;

        public InvalidSignatureFix(InvalidSignatureWarning warning)
        {
            myMethodSignature = warning.ExpectedMethodSignature;
            myMethodDeclaration = warning.MethodDeclaration;

            var parameters = string.Join(", ", myMethodSignature.Parameters.Select(p =>
                $"{p.Type.GetPresentableName(myMethodDeclaration.Language)} {p.Name}"));

            Text = $"Change parameters to '({parameters})'";
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var changeSignature = LanguageManager.Instance.TryGetService<ChangeSignature>(myMethodDeclaration.Language);
            if (changeSignature == null)
                return null;

            var model = changeSignature.CreateModel(myMethodDeclaration.DeclaredElement);
            for (var i = 0; i < myMethodSignature.Parameters.Length; i++)
            {
                var requiredParameter = myMethodSignature.Parameters[i];

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

            for (var i = model.ChangeSignatureParameters.Length - 1; i >= myMethodSignature.Parameters.Length; i--)
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

        public override string Text { get; }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return ValidUtils.Valid(myMethodDeclaration);
        }
    }
}