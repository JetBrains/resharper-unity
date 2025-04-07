using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public enum SerializedFieldRefactoringBehavior
    {
        Add,
        DontAdd,
        AddAndRemember,
        DontAddAndRemember,
    }

    public class SerializedFieldRenameModel
    {
        private readonly ISettingsStore mySettingsStore;

        public SerializedFieldRefactoringBehavior SerializedFieldRefactoringBehavior { get; private set; }

        public SerializedFieldRenameModel(ISettingsStore settingsStore)
        {
            mySettingsStore = settingsStore;
            var store = settingsStore.BindToContextTransient(ContextRange.ApplicationWide);
            var settings = store.GetValue((UnitySettings s) => s.SerializedFieldRefactoringSettings);

            SerializedFieldRefactoringBehavior = settings switch
            {
                SerializedFieldRefactoringSettings.AlwaysAdd => SerializedFieldRefactoringBehavior.AddAndRemember,
                SerializedFieldRefactoringSettings.NeverAdd => SerializedFieldRefactoringBehavior.DontAddAndRemember,
                _ => SerializedFieldRefactoringBehavior.Add
            };
        }

        public void Commit(bool shouldAddFormerlySerializedAs, bool rememberSelectedOptionAndNeverShowPopup)
        {
            SerializedFieldRefactoringBehavior = shouldAddFormerlySerializedAs 
                ? rememberSelectedOptionAndNeverShowPopup 
                    ? SerializedFieldRefactoringBehavior.AddAndRemember 
                    : SerializedFieldRefactoringBehavior.Add 
                : rememberSelectedOptionAndNeverShowPopup 
                    ? SerializedFieldRefactoringBehavior.DontAddAndRemember
                    : SerializedFieldRefactoringBehavior.DontAdd;
            
            var store = mySettingsStore.BindToContextTransient(ContextRange.ApplicationWide);
            var settings = SerializedFieldRefactoringBehavior switch
            {
                SerializedFieldRefactoringBehavior.AddAndRemember => SerializedFieldRefactoringSettings.AlwaysAdd,
                SerializedFieldRefactoringBehavior.DontAddAndRemember => SerializedFieldRefactoringSettings.NeverAdd,
                _ => SerializedFieldRefactoringSettings.ShowPopup
            };

            store.SetValue((UnitySettings s) => s.SerializedFieldRefactoringSettings, settings);
        }
    }
}