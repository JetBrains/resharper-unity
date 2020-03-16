using System;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
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
            var entry = GetIndexedEntry();
            if (entry == null)
                return;
            var path = (FileSystemPath.Parse(GetType().Assembly.Location).Parent / "Extensions/JetBrains.unity/dictionaries/unity.dic").FullPath;
            ScalarSettingsStoreAccess.SetIndexedValue(mountPoint, entry, path, null, true, null, myLogger);
        }

        [CanBeNull]
        private SettingsIndexedEntry GetIndexedEntry()
        {
            // TODO: Fix reference to Psi.Features.VisualStudio and use the InstalledDictionariesSettings type directly
            var type = Type.GetType(
                "JetBrains.PsiFeatures.VisualStudio.ReSpeller.InstalledDictionariesSettings, " +
                "JetBrains.PsiFeatures.VisualStudio.ReSpeller, Version=777.0.0.0, Culture=neutral, PublicKeyToken=1010a0d8d6380325");
            if (type == null)
            {
                myLogger.Warn("Unable to find InstalledDictionariesSettings type");
                return null;
            }
            var entry = mySettingsSchema.GetEntry(type, "InstalledDictionaries") as SettingsIndexedEntry;
            if (entry == null)
            {
                myLogger.Warn("Unable to find settings entry");
                return null;
            }
            return entry;
        }
    }
}