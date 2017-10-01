#if RIDER
using JetBrains.DataFlow;
using JetBrains.Platform.Unity.Model;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPluginTransmitter
    {
        public UnityPluginTransmitter(Lifetime lifetime, ILogger logger, SolutionModel solutionModel, UnityPluginProtocolController protocolController)
        {
            solutionModel.GetCurrentSolution().CustomData
                    .Data.Advise(lifetime, e =>
                {
                    if (e.Key == "UNITY_AttachEditorAndRun" && e.NewValue.ToLower()=="true" && e.NewValue!=e.OldValue)
                    {
                        logger.Verbose($"UNITY_AttachEditorAndRun {e.NewValue} came from frontend.");
                        var model = new UnityModel(lifetime, protocolController.Protocol);
                        model.Play.Value = true;
                    }
                });
        }
    }
}
#endif