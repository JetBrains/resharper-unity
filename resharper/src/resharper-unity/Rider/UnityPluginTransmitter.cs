#if RIDER
using JetBrains.DataFlow;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPluginTransmitter
    {
        public UnityPluginTransmitter(Lifetime lifetime, ILogger logger, Solution solution, UnityPluginProtocolController protocolController)
        {
            solution.CustomData
                    .Data.Advise(lifetime, e =>
                {
                    if (e.Key == "UNITY_AttachEditorAndRun" && e.NewValue.ToLower()=="true" && e.NewValue!=e.OldValue)
                    {
                        var pid = solution.CustomData.Data["UNITY_ProcessId"];
                        logger.Verbose($"UNITY_AttachEditorAndRun {e.NewValue} came from frontend. Pid: {pid}");
                        var model = new UnityModel(lifetime, protocolController.Protocol);
                        model.Play.Value = true;
                    }
                });
        }
    }
}
#endif