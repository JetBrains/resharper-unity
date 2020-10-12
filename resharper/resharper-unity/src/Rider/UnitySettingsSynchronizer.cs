using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnitySettingsSynchronizer
    {
        public UnitySettingsSynchronizer(Lifetime lifetime, ISolution solution, UnityHost host,
                                         IApplicationWideContextBoundSettingStore settingsStore)
        {
            var boundStore = settingsStore.BoundSettingsStore;
            var entry = boundStore.Schema.GetScalarEntry((UnitySettings s) => s.EnableShaderLabHippieCompletion);
            boundStore.GetValueProperty<bool>(lifetime, entry, null).Change.Advise_HasNew(lifetime, args =>
            {
                solution.Locks.ExecuteOrQueueEx(lifetime, "EnableShaderLabHippieCompletion", () =>
                    host.PerformModelAction(rd => rd.BackendSettings.EnableShaderLabHippieCompletion.Value = args.New));
            });

            var useYamlMergeSetting = boundStore.Schema.GetScalarEntry((UnitySettings s) => s.UseUnityYamlMerge);
            boundStore.GetValueProperty<bool>(lifetime, useYamlMergeSetting, null).Change.Advise_HasNew(lifetime, args =>
            {
                solution.Locks.ExecuteOrQueueEx(lifetime, "UseUnityYamlMerge", () =>
                    host.PerformModelAction(rd => rd.BackendSettings.UseUnityYamlMerge.Value = args.New));
            });

            var mergeParametersSetting = boundStore.Schema.GetScalarEntry((UnitySettings s) => s.MergeParameters);
            boundStore.GetValueProperty<string>(lifetime, mergeParametersSetting, null).Change.Advise_HasNew(lifetime, args =>
            {
                solution.Locks.ExecuteOrQueueEx(lifetime, "MergeParameters", () =>
                    host.PerformModelAction(rd => rd.BackendSettings.MergeParameters.Value = args.New));
            });

            var debuggerExtensionsEnabledSetting =
                boundStore.Schema.GetScalarEntry((UnitySettings s) => s.EnableDebuggerExtensions);
            boundStore.GetValueProperty<bool>(lifetime, debuggerExtensionsEnabledSetting, null).Change.Advise_HasNew(lifetime, args =>
            {
                solution.Locks.ExecuteOrQueueEx(lifetime, "DebuggerExtensionsEnabled", () =>
                    host.PerformModelAction(rd => rd.BackendSettings.EnableDebuggerExtensions.Value = args.New));
            });
        }
    }
}