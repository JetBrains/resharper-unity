using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;

[GeneratorElementProvider(GeneratorUnityKinds.UnityGenerateComponentReferences, typeof(CSharpLanguage))]
public class TestGenerateComponentReferencesProvider : TestGenerateDotsProviderBase
{
    public override double Priority => 1000;
}