﻿using JetBrains.ReSharper.Daemon.VisualElements;
using JetBrains.ReSharper.Feature.Services.ColorHints;
using JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Psi.Colors
{
    [RequireHlslSupport]
    [Category("ColorHighlighting")]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabColorHighlightingTests : ShaderLabHighlightingTestBase<ColorHintHighlighting>
    {
        protected override bool ColorIdentifiers => true;
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Colors";

        [Test] public void TestPropertyColor() { DoNamedTest2(); }
        [Test] public void TestColorValues() { DoNamedTest2(); }
        [Test] public void TestEdgeCasesAndErrors() { DoNamedTest2(); }

        [SetCulture("en-US")] [Test] public void TestPropertyColorCultureEn() { DoOneTest("PropertyColor"); }
        [SetCulture("en-US")] [Test] public void TestColorValuesCultureEn() { DoOneTest("ColorValues"); }
        [SetCulture("en-US")] [Test] public void TestEdgeCasesAndErrorsCultureEn() { DoOneTest("EdgeCasesAndErrors"); }

        [SetCulture("de-DE")] [Test] public void TestPropertyColorCultureDe() { DoOneTest("PropertyColor"); }
        [SetCulture("de-DE")] [Test] public void TestColorValuesCultureDe() { DoOneTest("ColorValues"); }
        [SetCulture("de-DE")] [Test] public void TestEdgeCasesAndErrorsCultureDe() { DoOneTest("EdgeCasesAndErrors"); }
    }
}