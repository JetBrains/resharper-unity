using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Settings
{
    [SolutionComponent]
    public class UiMinimizerSettings
    {
        public UiMinimizerSettings(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore)
        {
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;
            
            var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
            var hideDatabaseSetting = boundStore.GetValueProperty(lifetime, (UnitySettings s) => s.HideDataBaseToolWindow);
            var hideSolutionConfiguration = boundStore.GetValueProperty(lifetime,(UnitySettings s) => s.HideSolutionConfiguration);
            var rdUnityModel = solution.GetProtocolSolution().GetRdUnityModel();

            BindRdPropertyToProperty(lifetime, rdUnityModel.HideDataBaseToolWindow, hideDatabaseSetting);
            BindRdPropertyToProperty(lifetime, rdUnityModel.HideSolutionConfiguration, hideSolutionConfiguration);
        }

        private static void BindRdPropertyToProperty(Lifetime lifetime, IRdProperty<bool> rdProperty, IProperty<bool> property)
        {
            rdProperty.Value = property.Value;
            rdProperty.Advise(lifetime, value => property.SetValue(value));
            property.Change.Advise(lifetime, args =>
            {
                if (args.HasNew)
                    rdProperty.SetValue(args.New);
            });
        }
    }
}