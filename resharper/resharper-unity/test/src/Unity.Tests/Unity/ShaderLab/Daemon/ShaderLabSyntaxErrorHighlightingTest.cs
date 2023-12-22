﻿using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon
{
    [RequireHlslSupport]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabSyntaxErrorHighlightingTest : HighlightingTestBase
    {
        protected override PsiLanguageType? CompilerIdsLanguage => ShaderLabLanguage.Instance;
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\SyntaxHighlighting";

        [Test] public void TestSyntax01() { DoNamedTest2(); }
        [Test] public void TestSyntax02() { DoNamedTest2(); }
        [Test] public void TestSyntax03() { DoNamedTest2(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            return highlighting is ShaderLabHighlightingBase;
        }
    }
}