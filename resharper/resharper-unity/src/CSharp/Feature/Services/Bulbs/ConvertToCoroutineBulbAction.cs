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
    public class ConvertToCoroutineBulbAction : BulbActionBase
    {
        private readonly IMethodDeclaration myMethodDeclaration;

        public ConvertToCoroutineBulbAction(IMethodDeclaration methodDeclaration)
        {
            myMethodDeclaration = methodDeclaration;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var element = myMethodDeclaration.DeclaredElement;
            if (element == null) return null;

            var predefinedTypeCache = solution.GetComponent<IPredefinedTypeCache>();
            var predefinedType = predefinedTypeCache.GetOrCreatePredefinedType(myMethodDeclaration.GetPsiModule());

            var language = myMethodDeclaration.Language;
            var changeTypeHelper = LanguageManager.Instance.GetService<IChangeTypeHelper>(language);
            changeTypeHelper.ChangeType(predefinedType.IEnumerator, element);

            return null;
        }

        public override string Text => "To coroutine";
    }
}