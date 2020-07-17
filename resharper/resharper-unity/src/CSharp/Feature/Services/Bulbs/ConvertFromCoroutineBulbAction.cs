using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Bulbs
{
    public class ConvertFromCoroutineBulbAction : BulbActionBase
    {
        private readonly IMethodDeclaration myMethodDeclaration;

        public ConvertFromCoroutineBulbAction(IMethodDeclaration methodDeclaration)
        {
            myMethodDeclaration = methodDeclaration;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var element = myMethodDeclaration.DeclaredElement;
            if (element == null) return null;

            var unityApi = solution.GetComponent<UnityApi>();
            var eventFunction = unityApi.GetUnityEventFunction(element);

            var returnType = eventFunction.ReturnType.AsIType(myMethodDeclaration.GetPsiModule());

            var changeTypeHelper = LanguageManager.Instance.GetService<IChangeTypeHelper>(myMethodDeclaration.Language);
            changeTypeHelper.ChangeType(returnType, element);

            return null;
        }

        public override string Text => "To standard event function";
    }
}