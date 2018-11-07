using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.UserInterface;
using JetBrains.Application.Settings.UserInterface.FileInjectedLayers;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Settings.Store;
using JetBrains.ReSharper.Feature.Services.CSharp.Naming.AutoNaming;
using JetBrains.ReSharper.Features.XamlRendererHost.Preview;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class CodeStyleSettingsPatcher
    {
        private readonly Lifetime myLifetime;
        private readonly FileInjectedLayers myInjector;
        private readonly PluginPathsProvider myPathsProvider;
        private readonly UserInjectedSettingsLayers myInjectedSettingsLayers;
        private readonly IContextBoundSettingsStoreLive myContextBoundSettingsStoreLive;
        private readonly UserFriendlySettingsLayer.Identity mySettingsSolutionPersonalLayerId;
        private readonly FileSystemPath myPath;

        public CodeStyleSettingsPatcher(Lifetime lifetime, ISolution solution, UnitySolutionTracker unitySolutionTracker, SolutionSettings settings,
            ISettingsStore settingsStore, FileInjectedLayers injector, PluginPathsProvider pathsProvider, 
            UserInjectedSettingsLayers injectedSettingsLayers, CSharpAutoNamingDetection detection)
        {
            myLifetime = lifetime;
            myInjector = injector;
            myPathsProvider = pathsProvider;
            myInjectedSettingsLayers = injectedSettingsLayers;
            myContextBoundSettingsStoreLive = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
            mySettingsSolutionPersonalLayerId = settings.SolutionPersonalLayerId;
            myPath = pathsProvider.GetEditorPluginPathDir().Combine(PluginPathsProvider.UnityDotSettings);

            // if it is not Unity project, we should remove Unity-layer
            unitySolutionTracker.IsUnityProject.View(lifetime, (_, isUnity) =>
            {
                if (isUnity)
                {
                    if (!detection.ShouldAutoDetectionStarted())
                    {
                        SubscribeForInjection();
                    }
                    else
                    {
                        var entry = myContextBoundSettingsStoreLive.Schema.GetScalarEntry(
                            (CSharpAutoNamingSettings s) => s.IsNamingAutoDetectionCompleted);
                        myContextBoundSettingsStoreLive.GetValueProperty<bool>(myLifetime, entry, null).View(myLifetime,
                            (lt, value) =>
                            {
                                if (value)
                                {
                                    SubscribeForInjection();
                                }
                            });
                    }
                }
                else
                {
                    RemoveUnityLayer();
                }
            });
            

        }

        private void SubscribeForInjection()
        {
            var entry = myContextBoundSettingsStoreLive.Schema.GetScalarEntry((UnitySettings s) => s.EnableDefaultUnityCodeStyle);
            myContextBoundSettingsStoreLive.GetValueProperty<bool>(myLifetime, entry, null).View(myLifetime,
                (lifeTime, value) =>
                {
                    if (value)
                    {
                        InjectUnityLayer();
                    }
                    else
                    {
                        RemoveUnityLayer();
                    }
                }); 
        }

        private void InjectUnityLayer()
        {
            if (!IsLayerExists())
            {
                myInjector.InjectLayer(mySettingsSolutionPersonalLayerId, myPath);
            }
        }

        private void RemoveUnityLayer()
        {
            var unityLayer = GetUnityLayer();
            if (unityLayer != null)
            {
                myInjectedSettingsLayers.DeleteUserInjectedLayer(unityLayer.Id);
            }
        }

        private UserFriendlySettingsLayer GetUnityLayer()
        {
            return myInjectedSettingsLayers.GetAllUserInjectedLayers().Where(IsOurInjectedLayer).FirstOrDefault();
        }
        
        private bool IsOurInjectedLayer(UserFriendlySettingsLayer layer)
        {
            return layer.Name.Split("::").Last().EndsWith(PluginPathsProvider.UnityDotSettings);
        }
        
        private bool IsLayerExists()
        {
            return myInjector.IsLayerInjected(mySettingsSolutionPersonalLayerId, myPath);
        }
    }
}