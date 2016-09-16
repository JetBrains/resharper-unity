using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Generate
{
    public class GenerateMonoBehaviourMethodsWorkflow : GenerateCodeWorkflowBase
    {
        public GenerateMonoBehaviourMethodsWorkflow() : base(GeneratorUnityKinds.UnityMessages, null, "Unity3D Messages", GenerateActionGroup.CLR_LANGUAGE, "Unity3D Messages", "", "Generate.MonoBehaviour")
        {
        }

        public override double Order => 100;
    }
}