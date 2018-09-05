using JetBrains.ReSharper.Feature.Services.Naming;
using JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Psi.Naming.Elements
{
    [TestUnity]
    public class UnitySerializedFieldNameInspectionTests : CSharpHighlightingTestBase<InconsistentNamingWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\Naming\Elements";

        [Test] public void TestSerializedFieldNameWarnings01() { DoNamedTest2(); }
    }
}