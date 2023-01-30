using JetBrains.Application;
using JetBrains.ReSharper.TestFramework.Components.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRiderTestComponents
{
    // TODO TestIdGenerator is marker with ASP zone(???),
    // Also, IRequire in IUnityTestsZone for IPsiLanguageZone is not helping for discovering component for sdk test
    // while it working for dotnet-product case

    [ShellComponent]
    public class UnityTestIdGenerator : TestIdGenerator
    {
    }
}