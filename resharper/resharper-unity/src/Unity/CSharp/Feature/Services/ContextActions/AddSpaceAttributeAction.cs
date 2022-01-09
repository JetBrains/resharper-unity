using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Add 'Space' attribute",
        Description = "Add space before this property in the Unity Editor Inspector")]
    public class AddSpaceAttributeAction : AddInspectorAttributeAction
    {
        private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(BaseAnchor, SubmenuBehavior.Executable, LayoutPosition);

        public AddSpaceAttributeAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider, ourSubmenuAnchor)
        {
        }

        protected override IClrTypeName AttributeTypeName => KnownTypes.SpaceAttribute;
        protected override bool IsLayoutAttribute => true;
    }
}