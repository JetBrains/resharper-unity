using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Burst
{
    [TestUnity]
    public class BurstStageTest : UnityGlobalHighlightingsStageTestBase<IBurstHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\BurstCodeAnalysis\";

        [Test] public void SmartMarkingTests() { DoNamedTest(); }
        [Test] public void PrimitivesTests() { DoNamedTest(); }
        [Test] public void ReferenceExpressionTests() { DoNamedTest(); }
        [Test] public void MethodInvocationTests() { DoNamedTest(); }
        [Test] public void FunctionParametersReturnTests() { DoNamedTest(); }
        [Ignore("Try/finally, using and foreach are allowed fom burst 1.4")][Test] public void ExceptionsTests() { DoNamedTest(); }
        [Test] public void EqualsTests() { DoNamedTest(); }
        [Test] public void DirectivesTests() { DoNamedTest(); }
        [Test] public void BurstDiscardTests() { DoNamedTest(); }
        [Test] public void DebugStringTests() { DoNamedTest(); }
        [Test] public void TypeofTests() { DoNamedTest(); }
        [Test] public void SharedStaticCreateTests() { DoNamedTest(); }
        [Test] public void NullableTests() { DoNamedTest(); }
        [Test] public void ConditionalAttributesTests() { DoNamedTest(); }
        [Test] public void CommentRootsTests() { DoNamedTest(); }
        [Test] public void BugRider53010() { DoNamedTest(); }
        [Test] public void BugRider68193() { DoNamedTest(); }
        [Test] public void BugRider68095() { DoNamedTest(); }
        [Test] public void BugRider92491() { DoNamedTest(); }
        [Test] public void BugRider92491_2() { DoNamedTest(); }
        // Bug - youtrack
        // Issue - github.com/jetbrains/resharper-unity
        [Test] public void IssueRider2181() { DoNamedTest(); }
        
        [Test] public void BugRider106221() { DoNamedTest(); }
        
        [Test] public void BugRider113317WithoutBurst() { DoNamedTest(); }
    }
}