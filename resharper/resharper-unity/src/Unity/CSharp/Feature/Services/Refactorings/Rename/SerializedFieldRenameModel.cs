using JetBrains.Application.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class SerializedFieldRenameModel
    {
        private readonly ISettingsStore mySettingsStore;

        public bool ShouldAddFormerlySerializedAs;

        public SerializedFieldRenameModel(ISettingsStore settingsStore)
        {
            mySettingsStore = settingsStore;
            var store = settingsStore.BindToContextTransient(ContextRange.ApplicationWide);
            ShouldAddFormerlySerializedAs = store.GetValue((SerializedFieldRenameRefactoringSettings s) =>
                s.ShouldAddFormerlySerializedAs);
        }

        public void Commit(bool shouldAddFormerlySerializedAs)
        {
            ShouldAddFormerlySerializedAs = shouldAddFormerlySerializedAs;
            var store = mySettingsStore.BindToContextTransient(ContextRange.ApplicationWide);
            store.SetValue((SerializedFieldRenameRefactoringSettings s) => s.ShouldAddFormerlySerializedAs,
                ShouldAddFormerlySerializedAs);
        }
    }
}