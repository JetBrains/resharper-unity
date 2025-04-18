﻿#nullable enable

using System.Collections.Generic;
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
        private readonly IProperty<bool> myRememberSelectedOptionAndNeverShowPopup;

        public FormerlySerializedAsRefactoringPage(Lifetime lifetime, SerializedFieldRenameModel model,
            string fieldName)
            : base(lifetime)
        {
            myModel = model;

            myShouldAddFormerlySerializedAs = new Property<bool>(lifetime, "Should add attribute action",
                model.SerializedFieldRefactoringBehavior is SerializedFieldRefactoringBehavior.Add
                    or SerializedFieldRefactoringBehavior.AddAndRemember);

            myContent = myShouldAddFormerlySerializedAs.GetBeRadioGroup(lifetime,
                string.Format(Strings.UnitySettings_Refactoring_Popup_Should_Add_Attribute, fieldName),
                new List<bool> { true, false },
                present: (settings, properties) => settings
                    ? Strings.UnitySettings_Refactoring_Popup_Add
                    : Strings.UnitySettings_Refactoring_Popup_Dont_Add,
                horizontal: false
            ).InAutoGrid();


            myRememberSelectedOptionAndNeverShowPopup = new Property<bool>(lifetime, "Never show popup",
                model.SerializedFieldRefactoringBehavior
                    is SerializedFieldRefactoringBehavior.AddAndRemember
                    or SerializedFieldRefactoringBehavior.DontAddAndRemember);

            myContent.AddElement(new BeSpacer());
            myContent.AddElement(myRememberSelectedOptionAndNeverShowPopup.GetBeCheckBox(lifetime,
                Strings.UnitySettings_Refactoring_Popup_Remember_Selected_Options_And_Never_Show_Popup));
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
            var shouldAdd = myShouldAddFormerlySerializedAs.Value;
            var rememberSelectedOption = myRememberSelectedOptionAndNeverShowPopup.Value;

            myModel.Commit(shouldAdd, rememberSelectedOption);
        }
    }
}
