using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class TestableAssetSerializationMode : AssetSerializationMode
    {
        public TestableAssetSerializationMode(ISolution solution, ILogger logger)
            : base(solution, logger)
        {
            Mode = SerializationMode.ForceText;
        }

        public SerializationMode SetMode(SerializationMode mode)
        {
            var oldMode = Mode;
            Mode = mode;
            return oldMode;
        }
    }
}