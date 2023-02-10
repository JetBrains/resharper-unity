using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Color
{
    [TestUnity]
    public class SuppressMakeStaticMethodTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Dots";

        [Test] public void ISystemTest() { DoNamedTest(); }
        [Test] public void SystemBaseTest() { DoNamedTest(); }

        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            using (UnityPackageCookie.RunUnityPacakageCookie(Solution, PackageManager.UnityEntitiesPackageName))
                base.DoTest(lifetime, project);
        }
    }
}