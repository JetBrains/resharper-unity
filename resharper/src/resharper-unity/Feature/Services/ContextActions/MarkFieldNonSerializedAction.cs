using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Annotate field with 'NonSerialized' attribute",
        Description =
            "Adds 'NonSerializedAttribute' to a field in a known Unity type, marking the field as not serialized by Unity")]
    public class MarkFieldNonSerializedAction : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public MarkFieldNonSerializedAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public override string Text => "Annotate field with 'NonSerialized' attribute";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            if (!myDataProvider.Project.IsUnityProject())
                return false;

            var fieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            if (fieldDeclaration == null)
                return false;

            var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();

            var field = fieldDeclaration.DeclaredElement;
            return field != null && unityApi.IsUnityField(field);
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var fieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            AttributeUtil.AddAttribute(fieldDeclaration, PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS,
                myDataProvider.PsiModule, myDataProvider.ElementFactory);

            return null;
        }
    }
}