using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Add 'Tooltip' attribute",
        Description = "Add tooltip for this property in the Unity Editor Inspector")]
    public class AddTooltipAttributeAction : AddInspectorAttributeAction
    {
        private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(BaseAnchor, SubmenuBehavior.Executable, AnnotationPosition);

        public AddTooltipAttributeAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider, ourSubmenuAnchor)
        {
        }

        protected override IClrTypeName AttributeTypeName => KnownTypes.TooltipAttribute;
        protected override bool IsLayoutAttribute => false;

        // It makes no sense to apply Tooltip to each field in a multiple field declaration
        protected override bool SupportsSingleDeclarationOnly => true;

        protected override AttributeValue[] GetAttributeValues(IPsiModule module, IFieldDeclaration selectedFieldDeclaration)
        {
            return new[]
            {
                new AttributeValue(ConstantValue.String(selectedFieldDeclaration.DeclaredName, module))
            };
        }
    }
}