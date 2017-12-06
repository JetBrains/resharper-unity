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
        Name = "Annotate field with 'SerializeField' attribute",
        Description = "Adds 'UnityEngine.SerializeField' attribute to a field in a known Unity type, marking the field as serialized by Unity")]
    public class MarkSerializeFieldAction : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public MarkSerializeFieldAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public override string Text => "Annotate field with 'SerializeField' attribute";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            if (!myDataProvider.Project.IsUnityProject())
                return false;

            var fieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            if (fieldDeclaration == null)
                return false;

            // We can only apply the attribute to non-static, non-public fields
            // (private, protected, internal)
            if (fieldDeclaration.IsStatic || fieldDeclaration.GetAccessRights() == AccessRights.PUBLIC)
                return false;

            foreach (var attribute in fieldDeclaration.Attributes)
            {
                if (attribute.TypeReference?.Resolve().DeclaredElement is ITypeElement element)
                {
                    var attributName = element.GetClrName();
                    if (Equals(attributName, KnownTypes.SerializeField))
                        return false;

                    // TODO: Perhaps we should remove the NonSerialized attribute instead?
                    // Add a CA to convert to serialized field?
                    if (Equals(attributName, PredefinedType.NONSERIALIZED_ATTRIBUTE_CLASS))
                        return false;
                }
            }

            var typeDeclaration = fieldDeclaration.GetContainingTypeDeclaration();
            var typeElement = typeDeclaration?.DeclaredElement;
            if (typeElement == null)
                return false;

            // Is the type a Unity type?
            var unityApi = myDataProvider.Solution.GetComponent<UnityApi>();
            return unityApi.IsUnityType(typeElement);
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var fieldDeclaration = myDataProvider.GetSelectedElement<IFieldDeclaration>();
            AttributeUtil.AddAttribute(fieldDeclaration, KnownTypes.SerializeField,
                myDataProvider.PsiModule, myDataProvider.ElementFactory);

            return null;
        }
    }
}