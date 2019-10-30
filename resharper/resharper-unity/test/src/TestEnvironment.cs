using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel.NuGet;
using JetBrains.ReSharper.Daemon.SolutionAnalysis;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.ExternalSources;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.OptionPages;
using JetBrains.ReSharper.Feature.Services.ParameterInfo;
using JetBrains.ReSharper.Psi.Asp;
using JetBrains.ReSharper.Psi.Asp.Mvc;
using JetBrains.ReSharper.Psi.BuildScripts.MSBuild;
using JetBrains.ReSharper.Psi.BuildScripts.NAnt;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Css;
using JetBrains.ReSharper.Psi.IL;
using JetBrains.ReSharper.Psi.JavaScript;
using JetBrains.ReSharper.Psi.Protobuf;
using JetBrains.ReSharper.Psi.Razor;
using JetBrains.ReSharper.Psi.RegExp;
using JetBrains.ReSharper.Psi.Resx;
using JetBrains.ReSharper.Psi.VB;
using JetBrains.ReSharper.Psi.Xaml;
using JetBrains.ReSharper.TestFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Rider.Model;
using JetBrains.Symbols;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

#if RIDER
using JetBrains.ReSharper.Host.Env;
#endif

[assembly: RequiresThread(System.Threading.ApartmentState.STA)]

// This attribute is marked obsolete but is still supported. Use is discouraged in preference to convention, but the
// convention doesn't work for us. That convention is to walk up the tree from the executing assembly and look for a
// relative path called "test/data". This doesn't work because our common "build" folder is one level above our
// "test/data" folder, so it doesn't get found. We want to keep the common "build" folder, but allow multiple "modules"
// with separate "test/data" folders. E.g. "resharper-unity" and "resharper-yaml"
#pragma warning disable 618
[assembly: TestDataPathBase("resharper-unity/test/data")]
#pragma warning restore 618

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [ZoneDefinition]
    public interface IUnityTestZone :ITestsEnvZone, IRequire<PsiFeatureTestZone>
#if RIDER
        , IRequire<IRiderPlatformZone>
#endif
    {
    }

    [SetUpFixture]
    public class TestEnvironment : ExtensionTestEnvironmentAssembly<IUnityTestZone>
    {
    }
}
