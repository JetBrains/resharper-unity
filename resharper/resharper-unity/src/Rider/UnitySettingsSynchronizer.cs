using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnitySettingsSynchronizer
    {
        public UnitySettingsSynchronizer(Lifetime lifetime, ISolution solution, UnityHost host,
                                         ISettingsStore settingsStore)
        {
            var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
            var entry = boundStore.Schema.GetScalarEntry((UnitySettings s) => s.EnableShaderLabHippieCompletion);
            boundStore.GetValueProperty<bool>(lifetime, entry, null).Change.Advise(lifetime, args =>
            {
                if (args.HasNew)
                    host.PerformModelAction(rd => rd.EnableShaderLabHippieCompletion.Value = args.New);
            });

            var useYamlMergeSetting = boundStore.Schema.GetScalarEntry((UnitySettings s) => s.UseUnityYamlMerge);
            boundStore.GetValueProperty<bool>(lifetime, useYamlMergeSetting, null).Change.Advise(lifetime, args =>
            {
                if (args.HasNew)
                    host.PerformModelAction(rd => rd.UseUnityYamlMerge.Value = args.New);
            });
            
            var mergeParametersSetting = boundStore.Schema.GetScalarEntry((UnitySettings s) => s.MergeParameters);
            boundStore.GetValueProperty<string>(lifetime, mergeParametersSetting, null).Change.Advise(lifetime, args =>
            {
                if (args.HasNew)
                    host.PerformModelAction(rd => rd.MergeParameters.Value = args.New);
            });
        }
    }
}