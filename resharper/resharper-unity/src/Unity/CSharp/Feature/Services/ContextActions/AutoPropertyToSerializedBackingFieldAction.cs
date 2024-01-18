using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.BulbActions;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Util;
using JetBrains.ReSharper.Intentions.CSharp.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        ResourceType = typeof(Strings), NameResourceName = nameof(Strings.AutoPropertyToSerializedBackingFieldAction_Name), DescriptionResourceName = nameof(Strings.AutoPropertyToSerializedBackingFieldAction_Description),
        Priority = 2)]
    public class AutoPropertyToSerializedBackingFieldAction : ModernContextActionBase
    {
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public AutoPropertyToSerializedBackingFieldAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public override string Text => Strings.AutoPropertyToSerializedBackingFieldAction_Text_To_property_with_serialized_backing_field;

        public override bool IsAvailable(IUserDataHolder cache)
        {
            var propertyDeclaration = myDataProvider.GetSelectedElement<IPropertyDeclaration>();
            if (propertyDeclaration == null) return false;

            if (InterfaceDeclarationNavigator.GetByPropertyDeclaration(propertyDeclaration) != null) return false;

            if (!CSharpAutoPropertyUtil.IsPropertyHeaderSelected(propertyDeclaration, myDataProvider.SelectedTreeRange)) return false;
            if (!CSharpAutoPropertyUtil.IsEmptyOrNotImplemented(propertyDeclaration)) return false;

            return IsAvailable(propertyDeclaration);
        }

        protected override IBulbActionCommand ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var propertyDeclaration = myDataProvider.GetSelectedElement<IPropertyDeclaration>();
            return Execute(propertyDeclaration, myDataProvider.Solution, myDataProvider.ElementFactory);
        }

        public static bool IsAvailable([CanBeNull] IPropertyDeclaration propertyDeclaration)
        {
            if (propertyDeclaration == null)
                return false;

            if (!propertyDeclaration.IsFromUnityProject())
                return false;

            if (AutoPropertyToBackingFieldActionBase.IsAvailable(propertyDeclaration))
            {
                var unityApi = propertyDeclaration.GetSolution().GetComponent<UnityApi>();
                var containingType = propertyDeclaration.DeclaredElement?.GetContainingType();
                return containingType != null && unityApi.IsUnityType(containingType);
            }

            return false;
        }

        [CanBeNull]
        public static IBulbActionCommand Execute([CanBeNull] IPropertyDeclaration propertyDeclaration, ISolution solution, CSharpElementFactory elementFactory)
        {
            if (propertyDeclaration == null)
                return null;

            var fieldDeclaration = AutoPropertyToBackingFieldAction.Execute(propertyDeclaration);
            fieldDeclaration.SetReadonly(false);
            AttributeUtil.AddAttributeToSingleDeclaration(fieldDeclaration, KnownTypes.SerializeField, propertyDeclaration.GetPsiModule(), elementFactory);
            return AutoPropertyToBackingFieldAction.CreateHotspotsForFieldUsage(propertyDeclaration, fieldDeclaration, solution);
        }
    }
}