using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.Rider.Backend.Features.RunMarkers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.CSharp.Feature.RunMarkers
{
    [SolutionComponent]
    public class ShowMarkerOnStaticMethodSettings : IUnityProjectSettingsProvider
    {
        private readonly ISettingsSchema mySettingsSchema;
        private readonly ILogger myLogger;
        private readonly bool myInTests;

        public ShowMarkerOnStaticMethodSettings(ISettingsSchema settingsSchema, ILogger logger, bool inTests = false)
        {
            mySettingsSchema = settingsSchema;
            myLogger = logger;
            myInTests = inTests;
        }

        public void InitialiseProjectSettings(Lifetime projectLifetime, IProject project, ISettingsStorageMountPoint mountPoint)
        {
            if (myInTests)
                return;

            // hide, because for Unity projects this wouldn't work.
            // for Unity we have `UnityRunMarkerProvider` instead.
            var entry = mySettingsSchema.GetScalarEntry((RunMarkerSettings o) => o.ShowMarkerOnStaticMethods);
            ScalarSettingsStoreAccess.SetValue(mountPoint, entry, null, false, true, null, myLogger);
        }
    }
}