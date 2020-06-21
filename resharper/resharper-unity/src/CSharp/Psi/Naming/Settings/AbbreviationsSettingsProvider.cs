using System.IO;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp.Naming2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Settings
{
    [SolutionComponent]
    public class AbbreviationsSettingsProvider : IUnitySolutionSettingsProvider
    {
        private readonly ISettingsSchema mySettingsSchema;
        private readonly ILogger myLogger;

        public AbbreviationsSettingsProvider(ISettingsSchema settingsSchema, ILogger logger)
        {
            mySettingsSchema = settingsSchema;
            myLogger = logger;
        }

        public void InitialiseSolutionSettings(ISettingsStorageMountPoint mountPoint)
        {
            var streamName = GetType().Namespace + ".Abbreviations.txt";
            var stream = GetType().Assembly.GetManifestResourceStream(streamName);
            if (stream == null)
            {
                myLogger.Warn($"Cannot load resource stream: {streamName}");
                return;
            }

            using (var streamReader = new StreamReader(stream))
            {
                var entry = mySettingsSchema.GetIndexedEntry((CSharpNamingSettings s) => s.Abbreviations);

                string abbreviation;
                while ((abbreviation = streamReader.ReadLine()) != null)
                {
                    ScalarSettingsStoreAccess.SetIndexedValue(mountPoint, entry, abbreviation, null, abbreviation,
                        null, myLogger);
                }
            }
        }
    }
}