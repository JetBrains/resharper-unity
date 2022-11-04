using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class SerializedFieldRenameModel
    {
        private readonly ISettingsStore mySettingsStore;

        public bool ShouldAddFormerlySerializedAs { get; private set; }
        public bool DontShowPopup { get; private set; }

        public SerializedFieldRenameModel(ISettingsStore settingsStore)
        {
            mySettingsStore = settingsStore;
            var store = settingsStore.BindToContextTransient(ContextRange.ApplicationWide);
            ShouldAddFormerlySerializedAs =
                store.GetValue((UnitySettings s) => s.AddFormallySerializedAttributeOnRenaming);
            DontShowPopup = !store.GetValue((UnitySettings s) => s.ShowPopupForAddingFormallySerializedAttributeOnRenaming);
        }

        public void Commit(bool shouldAddFormerlySerializedAs, bool dontShowPopup)
        {
            ShouldAddFormerlySerializedAs = shouldAddFormerlySerializedAs;
            DontShowPopup = dontShowPopup;

            var store = mySettingsStore.BindToContextTransient(ContextRange.ApplicationWide);
            store.SetValue((UnitySettings s) => s.AddFormallySerializedAttributeOnRenaming,
                ShouldAddFormerlySerializedAs);
            store.SetValue((UnitySettings s) => s.ShowPopupForAddingFormallySerializedAttributeOnRenaming,
                !DontShowPopup);
        }
    }
}