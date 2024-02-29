using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis;

[TestUnity]
public class UnityObjectNullComparisonWarningTests : CSharpHighlightingTestBase<UnityObjectNullComparisonWarning>
{
    protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

    [Test] public void TestUnityObjectNullComparisonWarning() { DoNamedTest2(); }

    protected override void DoTest(Lifetime lifetime, IProject project)
    {
        using (project.GetComponent<UnityApi>().HasNullabilityAttributeOnImplicitBoolOperator.LocalReplaceCookie(x => x.Value, true))
            base.DoTest(lifetime, project);
    }
}