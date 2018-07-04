using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Util;
using JetBrains.ReSharper.Intentions.CSharp.ContextActions;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Replace auto-property with property and serialized backing field",
        Description = "Replaces an auto-property in a Unity type with a property that utilizes a backing field that is marked with the 'UnityEngine.SerializeField' attribute.",
        Priority = 2)]
    public class AutoPropertyToSerializedBackingFieldAction : ContextActionBase
    {
        public const string ActionText = "To property with serialized backing field";

        private readonly ICSharpContextActionDataProvider myDataProvider;

        public AutoPropertyToSerializedBackingFieldAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public override string Text => ActionText;

        public override bool IsAvailable(IUserDataHolder cache)
        {
            var propertyDeclaration = myDataProvider.GetSelectedElement<IPropertyDeclaration>();
            if (propertyDeclaration == null) return false;

            if (InterfaceDeclarationNavigator.GetByPropertyDeclaration(propertyDeclaration) != null) return false;

            if (!CSharpAutoPropertyUtil.IsPropertyHeaderSelected(propertyDeclaration, myDataProvider.SelectedTreeRange)) return false;
            if (!CSharpAutoPropertyUtil.IsEmptyOrNotImplemented(propertyDeclaration)) return false;

            return IsAvailable(propertyDeclaration);
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
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

            if (AutomaticToBackingFieldAction.IsAvailable(propertyDeclaration))
            {
                var unityApi = propertyDeclaration.GetSolution().GetComponent<UnityApi>();
                var containingType = propertyDeclaration.DeclaredElement?.GetContainingType();
                return containingType != null && unityApi.IsUnityType(containingType);
            }

            return false;
        }

        public static Action<ITextControl> Execute([CanBeNull] IPropertyDeclaration propertyDeclaration, ISolution solution, CSharpElementFactory elementFactory)
        {
            if (propertyDeclaration == null)
                return null;

            var fieldDeclaration = AutomaticToBackingFieldAction.Execute(propertyDeclaration);
            AttributeUtil.AddAttributeToSingleDeclaration(fieldDeclaration, KnownTypes.SerializeField, propertyDeclaration.GetPsiModule(), elementFactory);
            return AutomaticToBackingFieldAction.PostExecute(propertyDeclaration, fieldDeclaration, solution);
        }
    }
}