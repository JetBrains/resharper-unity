#if RIDER
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnitySettingsSynchronizer
    {
        public UnitySettingsSynchronizer(
            Lifetime lifetime,
            
            ISolution solution,
            SolutionModel solutionModel,
            ISettingsStore settingsStore)
        {
            var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
            var currentSolution = solutionModel.GetCurrentSolution();
            
            var entry = boundStore.Schema.GetScalarEntry((UnitySettings s) => s.EnableShaderLabHippieCompletion);
            boundStore.GetValueProperty<bool>(lifetime, entry, null).Change.Advise(lifetime, pcea =>
            {
                if (pcea.HasNew)
                {
                    currentSolution.CustomData.Data["UNITY_SETTINGS_EnableShaderLabHippieCompletion"] = pcea.New.ToString();
                }
            });
        }
    }
}
#endif