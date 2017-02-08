using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes
{
    [QuickFix]
    public class InvalidReturnTypeFix : QuickFixBase
    {
        private readonly IMethodDeclaration myMethodDeclaration;
        private readonly IType myReturnType;

        public InvalidReturnTypeFix(InvalidReturnTypeWarning warning)
        {
            var eventFunction = warning.Function;
            myMethodDeclaration = warning.MethodDeclaration;

            myReturnType = TypeFactory.CreateTypeByCLRName(eventFunction.ReturnType, myMethodDeclaration.GetPsiModule());
            if (eventFunction.ReturnTypeIsArray)
                myReturnType = TypeFactory.CreateArrayType(myReturnType, 1);
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var element = myMethodDeclaration.DeclaredElement;

            var language = myMethodDeclaration.Language;
            var changeTypeHelper = LanguageManager.Instance.GetService<IChangeTypeHelper>(language);
            changeTypeHelper.ChangeType(myReturnType, element);
            return null;
        }

        public override string Text
        {
            get
            {
                var returnType = myReturnType.GetPresentableName(myMethodDeclaration.Language);
                return $"Change return type to '{returnType}'";
            }
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return ValidUtils.Valid(myMethodDeclaration);
        }
    }
}