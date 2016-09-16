using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Generate
{
    public class GenerateUnityMessagesWorkflow : GenerateCodeWorkflowBase
    {
        public GenerateUnityMessagesWorkflow()
            : base(
                GeneratorUnityKinds.UnityMessages, null, "Unity3D Messages", GenerateActionGroup.CLR_LANGUAGE,
                "Unity3D Messages", "", "Generate.UnityMessage")
        {
        }

        public override double Order => 100;
    }
}