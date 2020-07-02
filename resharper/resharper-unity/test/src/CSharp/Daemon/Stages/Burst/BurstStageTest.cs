using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Burst
{   
    [TestUnity]
    public class BurstStageTest : UnityGlobalHighlightingsStageTestBase
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\BurstCodeAnalysis\";
        [Test] public void SmartMarkingTests() { DoNamedTest(); }
        [Test] public void PrimitivesTests() { DoNamedTest(); }
        [Test] public void ReferenceExpressionTests() { DoNamedTest(); }
        [Test] public void MethodInvocationTests() { DoNamedTest(); }
        [Test] public void FunctionParametersReturnTests() { DoNamedTest(); }
        [Test] public void ExceptionsTests() { DoNamedTest(); }
        [Test] public void EqualsTests() { DoNamedTest(); }
        [Test] public void DirectivesTests() { DoNamedTest(); }
        [Test] public void BurstDiscardTests() { DoNamedTest(); }
        [Test] public void DebugStringTests() { DoNamedTest(); }
        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile file, IContextBoundSettingsStore settingsStore)
        {
            return highlighting is BurstHighlighting;
        }
    }    
}    