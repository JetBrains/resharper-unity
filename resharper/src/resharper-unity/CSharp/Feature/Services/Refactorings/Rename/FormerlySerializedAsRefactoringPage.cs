using JetBrains.DataFlow;
using JetBrains.IDE.UI.Extensions;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.Rider.Model.UIAutomation;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class FormerlySerializedAsRefactoringPage : SingleBeRefactoringPage
    {
        private readonly SerializedFieldRenameModel myModel;
        private readonly BeGrid myContent;
        private readonly IProperty<bool> myShouldAddFormerlySerializedAs;

        public FormerlySerializedAsRefactoringPage(Lifetime lifetime, SerializedFieldRenameModel model)
            : base(lifetime)
        {
            myModel = model;
            myContent = new BeGrid(GridOrientation.Vertical);
            myShouldAddFormerlySerializedAs = new Property<bool>(lifetime, "ShouldAddAttribute", myModel.ShouldAddFormerlySerializedAs);
            var checkBox = myShouldAddFormerlySerializedAs.GetBeCheckBox(lifetime, "Add _FormerlySerializedAs attribute");
            myContent.AddElement(checkBox);
        }

        public override string Title => "Rename Unity serialized field";
        public override string Description => "Renaming a serialized field can break existing serialized data. The FormerlySerializedAs attribute tells Unity how to deserialize data with the old name.";
        public override BeControl GetPageContent() => myContent;

        public override void Commit()
        {
            myModel.Commit(myShouldAddFormerlySerializedAs.Value);
        }
    }
}