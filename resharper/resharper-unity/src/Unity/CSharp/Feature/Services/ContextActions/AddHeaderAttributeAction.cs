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
        Name = "Add 'Header' attribute",
        Description = "Add header for this property in the Unity Editor Inspector")]
    public class AddHeaderAttributeAction : AddInspectorAttributeAction
    {
        private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(BaseAnchor, SubmenuBehavior.Executable, LayoutPosition);

        public AddHeaderAttributeAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider, ourSubmenuAnchor)
        {
        }

        protected override IClrTypeName AttributeTypeName => KnownTypes.HeaderAttribute;
        protected override bool IsLayoutAttribute => true;

        protected override AttributeValue[] GetAttributeValues(IPsiModule module, IFieldDeclaration fieldDeclaration)
        {
            return new[]
            {
                new AttributeValue(ConstantValue.String(fieldDeclaration.DeclaredName, module))
            };
        }
    }
}