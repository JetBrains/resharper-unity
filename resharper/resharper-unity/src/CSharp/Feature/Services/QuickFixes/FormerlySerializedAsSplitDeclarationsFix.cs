using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.CSharp.ContextActions;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class FormerlySerializedAsSplitDeclarationsFix : QuickFixBase
    {
        private readonly IAttribute myAttribute;

        public FormerlySerializedAsSplitDeclarationsFix(PossibleMisapplicationOfAttributeToMultipleFieldsWarning warning)
        {
            myAttribute = warning.Attribute;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var attributeSectionList = AttributeSectionListNavigator.GetByAttribute(myAttribute);
            var multipleFieldDeclaration = MultipleFieldDeclarationNavigator.GetByAttributes(attributeSectionList);
            if (multipleFieldDeclaration != null)
                SplitDeclarationsListAction.Execute(multipleFieldDeclaration);
            return null;
        }

        public override string Text => "Split into separate declarations";
        public override bool IsAvailable(IUserDataHolder cache) => ValidUtils.Valid(myAttribute);
    }
}