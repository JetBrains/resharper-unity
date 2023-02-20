using System;
using System.Diagnostics;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration
{
    [SolutionComponent]
    public class UnityProcessTracker
    {
        public readonly IProperty<int?> UnityProcessId;

        public UnityProcessTracker(ILogger logger, Lifetime lifetime, BackendUnityHost backendUnityHost)
        {
            UnityProcessId = new Property<int?>("UnityProcessTracker.UnityProcessId");

            UnityProcessId.ForEachValue_NotNull(lifetime, (lt, processId) =>
            {
                var process = logger.CatchIgnore(() => Process.GetProcessById(processId.NotNull()));
                if (process == null)
                {
                    if (UnityProcessId.Value == processId) UnityProcessId.Value = null;
                    return;
                }

                process.EnableRaisingEvents = true;

                void OnProcessExited(object sender, EventArgs a)
                {
                    if (UnityProcessId.Value == processId) UnityProcessId.Value = null;
                }
                
                lt.Bracket(() => process.Exited += OnProcessExited, () => process.Exited -= OnProcessExited);

                if (process.HasExited)
                    if (UnityProcessId.Value == processId) UnityProcessId.Value = null;
            });

            backendUnityHost.BackendUnityModel!.ViewNotNull(lifetime, (lt, model) =>
            {
                // This will set the current value, if it exists
                model.UnityApplicationData.FlowInto(lt, UnityProcessId, data => data.UnityProcessId);
            });
        }
    }
}