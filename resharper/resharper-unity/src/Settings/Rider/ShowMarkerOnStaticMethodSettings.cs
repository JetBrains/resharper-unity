using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.RunMarkers;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [SolutionComponent]
    public class ShowMarkerOnStaticMethodSettings : IUnityProjectSettingsProvider
    {
        private readonly ISettingsStore mySettingsStore;

        public ShowMarkerOnStaticMethodSettings(ISettingsStore settingsStore)
        {
            mySettingsStore = settingsStore;
        }

        public void InitialiseProjectSettings(Lifetime projectLifetime, IProject project, ISettingsStorageMountPoint mountPoint)
        {
            var store = mySettingsStore.BindToMountPoints(new[] {mountPoint});
            store.SetValue<RunMarkerSettings, bool>(s => s.ShowMarkerOnStaticMethods, false);
        }
    }
}