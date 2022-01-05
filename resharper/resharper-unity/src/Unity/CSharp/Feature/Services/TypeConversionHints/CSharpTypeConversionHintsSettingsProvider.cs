using JetBrains.Application.InlayHints;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.TypeConversionHints;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.TypeConversionHints
{
    [SolutionComponent]
    public class CSharpTypeConversionHintsSettingsProvider : IUnitySolutionSettingsProvider
    {
        private readonly ISettingsSchema mySettingsSchema;
        private readonly ILogger myLogger;

        public CSharpTypeConversionHintsSettingsProvider(ISettingsSchema settingsSchema, ILogger logger)
        {
            mySettingsSchema = settingsSchema;
            myLogger = logger;
        }

        public void InitialiseSolutionSettings(ISettingsStorageMountPoint mountPoint)
        {
            // Show type conversion hints always. The default is push-to-hint, but the big benefit for Unity developers
            // is to see when an implicit conversion is happening between Vector2 and Vector3, as this is a lossy conversion
            // This can still be overridden at the solution or personal level
            var entry = mySettingsSchema.GetScalarEntry((CSharpTypeConversionHintsOptions o) => o.ShowTypeConversionHints);
            ScalarSettingsStoreAccess.SetValue(mountPoint, entry, null, InlayHintsMode.Always, true, null, myLogger);

            // Show as a hint, not as an icon
            entry = mySettingsSchema.GetScalarEntry((CSharpTypeConversionHintsOptions o) =>
                o.ShowHintsInSimplifiedMode);
            ScalarSettingsStoreAccess.SetValue(mountPoint, entry, null, false, true, null, myLogger);
        }
    }
}