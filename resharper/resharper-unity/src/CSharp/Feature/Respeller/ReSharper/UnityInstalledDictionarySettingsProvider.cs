using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
// using JetBrains.PsiFeatures.VisualStudio.ReSpeller;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Respeller
{
    [SolutionComponent]
    public class UnityInstalledDictionarySettingsProvider : IUnitySolutionSettingsProvider
    {
        private readonly ISettingsSchema mySettingsSchema;
        private readonly ILogger myLogger;

        public UnityInstalledDictionarySettingsProvider(ISettingsSchema settingsSchema, ILogger logger)
        {
            mySettingsSchema = settingsSchema;
            myLogger = logger;
        }

        public void InitialiseSolutionSettings(ISettingsStorageMountPoint mountPoint)
        {
            // var entry = mySettingsSchema.GetIndexedEntry((InstalledDictionariesSettings s) => s.InstalledDictionaries);
            // var path = (FileSystemPath.Parse(GetType().Assembly.Location).Parent / "Extensions/JetBrains.unity/dictionaries/unity.dic").FullPath;
            // ScalarSettingsStoreAccess.SetIndexedValue(mountPoint, entry, path, null, true, null, myLogger);
        }
    }
}