using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.InplaceRefactorings;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Refactorings.ChangeSignature;
using JetBrains.TextControl;
using JetBrains.Util;

#if WAVE07 || WAVE08
using JetBrains.ActionManagement;
using JetBrains.UI.ActionsRevised.Handlers;
using JetBrains.UI.ActionSystem;
#else
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Handlers;
using JetBrains.Application.UI.ActionSystem;
#endif

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes
{
    [QuickFix]
    public class InvalidSignatureFix : QuickFixBase
    {
        private readonly IMethodDeclaration myMethodDeclaration;
        private readonly UnityEventFunction myEventFunction;

        public InvalidSignatureFix(InvalidSignatureWarning warning)
        {
            myEventFunction = warning.Function;
            myMethodDeclaration = warning.MethodDeclaration;

            var parameters = string.Join(", ", myEventFunction.Parameters.Select(p =>
                string.Format("{0} {1}",
                    CreateParameterType(p).GetPresentableName(myMethodDeclaration.Language),
                    p.Name)));

            Text = $"Change parameters to '({parameters})'";
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var changeSignature = LanguageManager.Instance.TryGetService<ChangeSignature>(myMethodDeclaration.Language);
            if (changeSignature == null)
                return null;

            var model = changeSignature.CreateModel(myMethodDeclaration.DeclaredElement);
            for (var i = 0; i < myEventFunction.Parameters.Length; i++)
            {
                var requiredParameter = myEventFunction.Parameters[i];

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
                modelParameter.ParameterType = CreateParameterType(requiredParameter);

                // Reset everything else
                modelParameter.DefaultValue = null;
                modelParameter.IsOptional = false;
                modelParameter.IsParams = false;
                modelParameter.IsThis = false;
                modelParameter.IsVarArg = false;
            }

            for (var i = model.ChangeSignatureParameters.Length - 1; i >= myEventFunction.Parameters.Length; i--)
                model.Remove(i);

            var refactoring = new ChangeSignatureRefactoring(model);
            refactoring.Execute(NullProgressIndicator.Instance);

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

        private ChangeSignatureParameter FindBestMatch(UnityEventFunctionParameter requiredParameter, ChangeSignatureModel model, int i)
        {
            // Try and match type and name first
            for (var j = i; j < model.ChangeSignatureParameters.Length; j++)
            {
                if (model.ChangeSignatureParameters[j].ParameterName == requiredParameter.Name
                    && DoTypesMatch(model.ChangeSignatureParameters[j].ParameterType, requiredParameter))
                {
                    return model.ChangeSignatureParameters[j];
                }
            }

            // Now just match type - we'll update name after
            for (var j = i; j < model.ChangeSignatureParameters.Length; j++)
            {
                if (DoTypesMatch(model.ChangeSignatureParameters[j].ParameterType, requiredParameter))
                {
                    return model.ChangeSignatureParameters[j];
                }
            }

            return null;
        }

        private bool DoTypesMatch(IType parameterType, UnityEventFunctionParameter requiredParameter)
        {
            if (requiredParameter.IsArray && parameterType is IArrayType)
            {
                var arrayType = (IArrayType) parameterType;
                parameterType = arrayType.ElementType;
            }

            var typeElement = parameterType.GetTypeElement();
            if (typeElement == null)
                return false;
            return Equals(typeElement.GetClrName(), requiredParameter.ClrTypeName);
        }

        private IType CreateParameterType(UnityEventFunctionParameter parameter)
        {
            var type = TypeFactory.CreateTypeByCLRName(parameter.ClrTypeName, myMethodDeclaration.GetPsiModule());
            if (parameter.IsArray)
                return TypeFactory.CreateArrayType(type, 1);
            return type;
        }

        public override string Text { get; }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return ValidUtils.Valid(myMethodDeclaration);
        }
    }
}