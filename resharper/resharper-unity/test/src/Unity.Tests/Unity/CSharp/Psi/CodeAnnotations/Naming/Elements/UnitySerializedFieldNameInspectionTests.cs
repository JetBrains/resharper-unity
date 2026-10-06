using JetBrains.ReSharper.Daemon.Specific.NamingConsistencyCheck;
using JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Psi.CodeAnnotations.Naming.Elements
{
    [TestUnity]
    public class UnitySerializedFieldNameInspectionTests : CSharpHighlightingTestBase<InconsistentNamingWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\Naming\Elements";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Serialization", "Serialized field naming")] public void TestSerializedFieldNameWarnings01() { DoNamedTest2(); }
    }
}