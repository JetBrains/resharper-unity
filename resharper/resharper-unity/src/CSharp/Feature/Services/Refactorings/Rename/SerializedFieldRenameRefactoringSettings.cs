using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Refactorings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    [SettingsKey(typeof(RefactoringsMruSettings), "Unity SerializedField rename refactoring settings")]
    public class SerializedFieldRenameRefactoringSettings
    {
        [SettingsEntry(true, "Whether to add the FormerlySerializedAs attribute")]
        public bool ShouldAddFormerlySerializedAs;
    }
}