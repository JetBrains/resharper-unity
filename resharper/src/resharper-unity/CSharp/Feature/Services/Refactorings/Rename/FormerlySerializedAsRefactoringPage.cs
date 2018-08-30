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
            myShouldAddFormerlySerializedAs = new Property<bool>(lifetime, "ShouldAddAttribute", myModel.ShouldAddFormerlySerializedAs);
            myContent = myShouldAddFormerlySerializedAs.GetBeCheckBox(lifetime, "Add _FormerlySerializedAs attribute").InAutoGrid();
        }

#if RESHARPER
        // ReSharper has a layout issue where the page doesn't resize properly. Reducing the description to a single
        // line is the best workaround for now
        public override string Title => "Renaming a serialized field can break existing serialized data";
        public override string Description => "Adding the FormerlySerializedAs attribute tells Unity to also try the old name.";
#else
        // And Rider doesn't show the title, only the description
        public override string Title => "Rename Unity serialized field";
        public override string Description => "Renaming a serialized field can break existing serialized data. Adding the FormerlySerializedAs attribute tells Unity to also try the old name.";
#endif

        public override BeControl GetPageContent() => myContent;

        public override void Commit()
        {
            myModel.Commit(myShouldAddFormerlySerializedAs.Value);
        }
    }
}