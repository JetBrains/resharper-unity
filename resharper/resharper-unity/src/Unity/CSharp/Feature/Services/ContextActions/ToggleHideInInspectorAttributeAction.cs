using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Toggle 'HideInInspector' attribute on fields",
        Description =
            "Adds or removes the 'HideInInspector' attribute on a Unity serialized field, removing the field from the Inspector window.")]
    public class ToggleHideInInspectorAttributeAction : AddInspectorAttributeAction
    {
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(BaseAnchor, SubmenuBehavior.Executable, AnnotationPosition);

        public ToggleHideInInspectorAttributeAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider, ourSubmenuAnchor)
        {
        }

        protected override IClrTypeName AttributeTypeName => KnownTypes.HideInInspectorAttribute;
        protected override bool IsRemoveActionAvailable => true;
        protected override bool IsLayoutAttribute => false;
    }
}