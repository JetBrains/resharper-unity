#if RIDER
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnitySettingsSynchronizer : UnityReferencesTracker.IHandler
    {
        private readonly Lifetime myLifetime;
        private readonly IContextBoundSettingsStoreLive myBoundStore;
        private readonly SolutionModel mySolutionModel;

        public UnitySettingsSynchronizer(
            Lifetime lifetime,
            ISolution solution,
            SolutionModel solutionModel,
            ISettingsStore settingsStore)
        {
            myLifetime = lifetime;
            mySolutionModel = solutionModel;
            myBoundStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
        }

        public void OnReferenceAdded(IProject unityProject, Lifetime projectLifetime)
        {
        }

        public void OnSolutionLoaded(UnityProjectsCollection solution)
        {
            var entry = myBoundStore.Schema.GetScalarEntry((UnitySettings s) => s.EnableShaderLabHippieCompletion);
            myBoundStore.GetValueProperty<bool>(myLifetime, entry, null).Change.Advise(myLifetime, pcea =>
            {
                if (pcea.HasNew)
                {
                    if (mySolutionModel.HasCurrentSolution()) // in tests we don't have one
                    {
                        mySolutionModel.GetCurrentSolution()
                            .CustomData
                            .Data["UNITY_SETTINGS_EnableShaderLabHippieCompletion"] = pcea.New.ToString();
                    }
                }
            });
        }
    }
}
#endif