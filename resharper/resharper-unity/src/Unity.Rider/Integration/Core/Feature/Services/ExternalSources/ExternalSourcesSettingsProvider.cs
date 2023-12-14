using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ExternalSource;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Pdb;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Services.ExternalSources
{
    // Read only packages are compiled and turned into assembly references. We show them as source in the Unity Explorer
    // but can only navigate to the symbols in the referenced assemblies. This class ensures that the External Sources
    // settings are correctly set to navigate to source from PDB files.
    // Since ReSharper does not have a Unity Explorer view, it is harder to navigate to package sources, so we won't
    // override the user's settings. (RIDER-90596)
    [SolutionComponent]
    public class ExternalSourcesSettingsProvider : IUnitySolutionSettingsProvider
    {
        private readonly ISettingsSchema mySettingsSchema;
        private readonly ILogger myLogger;

        public ExternalSourcesSettingsProvider(ISettingsSchema settingsSchema, ILogger logger)
        {
            mySettingsSchema = settingsSchema;
            myLogger = logger;
        }

        public void InitialiseSolutionSettings(ISettingsStorageMountPoint mountPoint)
        {
            // Navigate to source for external symbols (ensure default value is still set)
            var entry = mySettingsSchema.GetScalarEntry((ExternalSourcesSettings s) => s.NavigationMode);
            ScalarSettingsStoreAccess.SetValue(mountPoint, entry, null, ExternalSourcesNavigationMode.Sources, true,
                null, myLogger);

            // Use source files from PDBs, if available (ensure default value is still set)
            // This is the important setting. Without this, we would show decompiled code, even though we can navigate
            // to source from the Unity Explorer
            entry = mySettingsSchema.GetScalarEntry((PdbNavigationSettings s) => s.UseSymbolFiles);
            ScalarSettingsStoreAccess.SetValue(mountPoint, entry, null, true, true, null, myLogger);

            // Allow navigating to private and internal package symbols (not the default)
            entry = mySettingsSchema.GetScalarEntry((ExternalSourcesSettings s) => s.ShowNonPublicCompiledElementsInGoto);
            ScalarSettingsStoreAccess.SetValue(mountPoint, entry, null, true, true, null, myLogger);
        }
    }
}
