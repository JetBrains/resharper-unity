using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel.Update;
using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Tests.AsmDefCommon.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDefNew.Psi.Resolve
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferenceTests : AsmDefReferenceTestsBase<AsmDefNameReference>
    {

        [Test] public void TestUnresolvedReference01() { DoNamedTest2(); }
        [Test] public void TestUnresolvedReference02() { DoNamedTest2("UnresolvedReference02_SecondProject.asmdef"); }
        [Test] public void TestCrossProjectReference() { DoNamedTest2("CrossProjectReference_SecondProject.asmdef");}
        [Test] public void TestCorrectJsonReferences() { DoNamedTest2(); }
    }
}