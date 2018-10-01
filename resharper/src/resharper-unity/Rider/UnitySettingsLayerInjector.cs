using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.UserInterface;
using JetBrains.Application.Settings.UserInterface.FileInjectedLayers;
using JetBrains.DataFlow;
using JetBrains.Platform.MsBuildTask.Utils;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Settings.Store;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnitySettingsLayerInjector
    {
        private readonly FileInjectedLayers myInjector;
        private readonly PluginPathsProvider myPathsProvider;
        private readonly UserInjectedSettingsLayers myInjectedSettingsLayers;
        private readonly UserFriendlySettingsLayer.Identity mySettingsSolutionSharedLayerId;
        private readonly FileSystemPath myPath;

        public UnitySettingsLayerInjector(Lifetime lifetime, UnitySolutionTracker tracker, ISolution solution, SolutionSettings settings,
            ISettingsStore settingsStore, FileInjectedLayers injector, PluginPathsProvider pathsProvider, UserInjectedSettingsLayers injectedSettingsLayers)
        {
            myInjector = injector;
            myPathsProvider = pathsProvider;
            myInjectedSettingsLayers = injectedSettingsLayers;
            var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
            mySettingsSolutionSharedLayerId = settings.SolutionSharedLayerId;
            myPath = myPathsProvider.GetEditorPluginPathDir().Combine(PluginPathsProvider.UnityDotSettings);

            var entry = boundStore.Schema.GetScalarEntry((UnitySettings s) => s.EnableDefaultUnityCodeStyle);
            boundStore.GetValueProperty<bool>(lifetime, entry, null).Change.Advise(lifetime,
                value =>
                {
                    if (value.HasNew)
                    {
                        HandleInjectLayer(value.New);
                    }
                }); 
        }

        private void HandleInjectLayer(bool isDefaultCodeStyleEnabled)
        {
            if (isDefaultCodeStyleEnabled)
            {
                InjectLayer();
            }
            else
            {
                RemoveLayer();
            }
        }
        
        private void InjectLayer()
        {
            if (!IsLayerExists())
            {
                myInjector.InjectLayer(mySettingsSolutionSharedLayerId, myPath);
            }
        }
        
        private void RemoveLayer()
        {
            if (IsLayerExists())
            {
                var layers = myInjectedSettingsLayers.GetAllUserInjectedLayers();
                var identity = myInjectedSettingsLayers.GetAllUserInjectedLayers().Where(IsOurInjectedLayer).Select(t => t.Id).First();
                myInjectedSettingsLayers.DeleteUserInjectedLayer(identity);
            }
        }

        private bool IsOurInjectedLayer(UserFriendlySettingsLayer layer)
        {
            return layer.Name.Split("::").Last().Equals(
                myPathsProvider.GetEditorPluginPathDir().Combine(PluginPathsProvider.UnityDotSettings).FullPath);
        }

        private bool IsLayerExists()
        {
            return myInjector.IsLayerInjected(mySettingsSolutionSharedLayerId, myPath);
        }
    }
}