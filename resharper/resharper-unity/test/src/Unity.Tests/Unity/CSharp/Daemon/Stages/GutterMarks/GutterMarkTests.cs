using JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [TestUnity]
    public class GutterMarkTests : CSharpHighlightingTestBase<IUnityIndicatorHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\GutterMark";

        // ********************************************************************
        // IMPORTANT! Keep in sync with equivalent class in Unity.Rider.Tests
        // ********************************************************************

        [Test] public void Test01() { DoNamedTest(); }

        [Test, TestUnity(UnityVersion.Unity2019_4)] public void TestGenericSerialisedFields_2019_4() { DoNamedTest2(); }
        [Test, TestUnity(UnityVersion.Unity2020_1)] public void TestGenericSerialisedFields_2020_1() { DoNamedTest2(); }

        // Unity 6.6 (6000.6) added built-in Dictionary serialization, opt-in via [SerializeField]. Before 6.6 no
        // dictionary field is serialized at all, so the two gold files below must differ.
        [Test, TestUnity(UnityVersion.Unity2022_3)] public void TestSerialisedDictionaryFields_2022_3() { DoNamedTest2(); }
        [Test, TestUnity(UnityVersion.Unity6000_6)] public void TestSerialisedDictionaryFields_6000_6() { DoNamedTest2(); }

        [Test] public void OdinSerialisedFields() { DoNamedTest(); }

        // Odin has always serialized dictionaries. From Unity 6.6 both mechanisms apply, so check they coexist.
        [Test, TestUnity(UnityVersion.Unity6000_6)] public void TestOdinSerialisedDictionaryFields_6000_6() { DoNamedTest2(); }
    }
}