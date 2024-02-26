using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [GeneratorElementProvider(GeneratorUnityKinds.UnityGenerateRefAccessors, typeof(CSharpLanguage))]
    public class TestGenerateRefAccessorsProvider : TestGenerateDotsProviderBase
    {
        public override double Priority => 1000;

    }
}