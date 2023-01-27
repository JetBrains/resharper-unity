#nullable enable

using JetBrains.DataFlow;
using JetBrains.IDE.UI.Extensions;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.Rider.Model.UIAutomation;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class FormerlySerializedAsRefactoringPage : SingleBeRefactoringPage
    {
        private readonly SerializedFieldRenameModel myModel;
        private readonly BeGrid myContent;
        private readonly IProperty<bool> myShouldAddFormerlySerializedAs;
        private readonly IProperty<bool> myDontShowPopup;

        public FormerlySerializedAsRefactoringPage(Lifetime lifetime, SerializedFieldRenameModel model)
            : base(lifetime)
        {
            myModel = model;
            myShouldAddFormerlySerializedAs = new Property<bool>(Strings.UnitySettings_Refactoring_Add_Formally_Serialized_As_Attribute_while_renaming_Serialized_Property, myModel.ShouldAddFormerlySerializedAs);
            myContent = myShouldAddFormerlySerializedAs.GetBeCheckBox(lifetime, Strings.UnitySettings_Refactoring_Add_Formally_Serialized_As_Attribute_while_renaming_Serialized_Property).InAutoGrid();

            myDontShowPopup = new Property<bool>(Strings.UnitySettings_Refactoring_Dont_shot_popup_Add_Formally_Serialized_As_Attribute_while_renaming_Serialized_Property, myModel.DontShowPopup);
            myContent.AddElement(myDontShowPopup
                .GetBeCheckBox(lifetime, Strings.UnitySettings_Refactoring_Dont_shot_popup_Add_Formally_Serialized_As_Attribute_while_renaming_Serialized_Property));
        }

#if RESHARPER
        // ReSharper has a layout issue where the page doesn't resize properly. Reducing the description to a single
        // line is the best workaround for now
        public override string Title => "Renaming a serialized field can break existing serialized data";
        public override string Description => "Adding the 'FormerlySerializedAs' attribute tells Unity to also try the old name.";
#else
        // And Rider doesn't show the title, only the description
        public override string Title => "Rename Unity serialized field";
        public override string Description => "Renaming a serialized field can break existing serialized data. Adding the 'FormerlySerializedAs' attribute tells Unity to also try the old name.";
#endif

        public override BeControl GetPageContent() => myContent;

        public override void Commit()
        {
            myModel.Commit(myShouldAddFormerlySerializedAs.Value, myDontShowPopup.Value);
        }
    }
}
