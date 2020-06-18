using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Add 'Range' attribute",
        Description = "Add range for this property in the Unity Editor Inspector")]
    public class AddRangeAttributeAction : AddInspectorAttributeAction
    {
        private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(BaseAnchor, SubmenuBehavior.Executable, AnnotationPosition);

        public AddRangeAttributeAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider, ourSubmenuAnchor)
        {
        }

        protected override IClrTypeName AttributeTypeName => KnownTypes.RangeAttribute;
        protected override bool IsLayoutAttribute => false;

        // It can make sense to apply Range to all of the fields in a multiple field declaration, but it's unintuitive,
        // especially as we're dealing with validation. Don't suggest it, but don't warn about it either.
        protected override bool SupportsSingleDeclarationOnly => true;

        protected override AttributeValue[] GetAttributeValues(IPsiModule module, IFieldDeclaration fieldDeclaration)
        {
            return new[]
            {
                new AttributeValue(new ConstantValue(0, module)),
                new AttributeValue(new ConstantValue(1, module))
            };
        }
    }
}