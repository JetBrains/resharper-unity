using System;
using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Utils;
using JetBrains.Text;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Psi.ShaderLab.Parsing
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADER_EXTENSION)]
    public class ParserTests : ParserTestBase<ShaderLabLanguage>
    {
        protected override string RelativeTestDataPath => @"psi\shaderLab\parsing";

        [TestCase("First")]

        [TestCase("PropertiesEmpty")]
        [TestCase("Properties")]
        [TestCase("PropertiesWithAttributes")]
        [TestCase("PropertiesWithErrors")]

        [TestCase("FallbackNamed")]
        [TestCase("FallbackNone")]
        [TestCase("FallbackError")]

        [TestCase("CustomEditor")]
        [TestCase("CustomEditorError")]

        [TestCase("Dependency01")]
        [TestCase("Dependency02")]
        [TestCase("DependencyErrors")]

        [TestCase("SubShader01")]
        [TestCase("SubShader02")]
        [TestCase("SubShaderTags")]

        [TestCase("PassDefGrabPass")]
        [TestCase("PassDefUsePass")]

        [TestCase("CullDepth01")]
        [TestCase("PassTags")]
        [TestCase("Stencil01")]
        [TestCase("Stencil02")]
        [TestCase("ColorMask")]
        [TestCase("Name")]
        [TestCase("LOD")]

        [TestCase("Blending")]

        [TestCase("LegacyLighting01")]
        [TestCase("LegacyLighting02")]
        [TestCase("LegacyLighting03")]
        [TestCase("LegacyLighting04")]

        [TestCase("LegacyTextureCombiner01")]
        [TestCase("LegacyTextureCombiner02")]
        [TestCase("LegacyTextureCombiner03")]
        [TestCase("LegacyTextureCombiner04")]

        [TestCase("LegacyAlphaTesting01")]
        [TestCase("LegacyAlphaTesting02")]
        [TestCase("LegacyAlphaTesting03")]
        [TestCase("LegacyAlphaTesting04")]

        [TestCase("LegacyFog")]

        [TestCase("LegacyBindChannels")]
        public void TestParser(string name) => DoOneTest(name);
    }

    [Explicit]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADER_EXTENSION)]
    public class ParseBuiltinShaderTests : BaseTestWithSingleProject
    {
        // NOTE: Requires downloading the built in shaders from Unity
        protected override string RelativeTestDataPath => @"psi\shaderLab\parsing\external\builtin_shaders-5.5.1f1";

        [TestCaseSource(nameof(BuiltinShadersFolder))]
        public void TestBuiltinShaders(string name) => DoOneTest(name);

        private IEnumerable<string> BuiltinShadersFolder
        {
            get
            {
                TestUtil.SetHomeDir(GetType().Assembly);

                var path = TestDataPath2;
                foreach (var file in path.GetChildFiles("*.shader", PathSearchFlags.RecurseIntoSubdirectories))
                    yield return file.MakeRelativeTo(TestDataPath2).ChangeExtension(string.Empty).FullPath;
            }
        }

        protected override void DoTest(IProject testProject)
        {
            var projectFile = testProject.GetAllProjectFiles().FirstNotNull();

            var text = projectFile.Location.ReadAllText2();
            var buffer = new StringBuffer(text.Text);

            var languageService = ShaderLabLanguage.Instance.LanguageService();
            var lexer = languageService.GetPrimaryLexerFactory().CreateLexer(buffer);
            var psiModule = Solution.PsiModules().GetPrimaryPsiModule(testProject, TargetFrameworkId.Default);
            var parser = languageService.CreateParser(lexer, psiModule, null);
            var psiFile = parser.ParseFile();

            if (DebugUtil.HasErrorElements(psiFile))
            {
                DebugUtil.DumpPsi(Console.Out, psiFile);
                Assert.Fail("File contains error elements");
            }

            Assert.AreEqual(text.Text, psiFile.GetText(), "Reconstructed text mismatch");
            //CheckRange(buffer, psiFile);
        }

        private static void CheckRange(string documentText, ITreeNode node)
        {
            Assert.AreEqual(node.GetText(), documentText.Substring(node.GetTreeStartOffset().Offset, node.GetTextLength()), "node range text mismatch");

            for (var child = node.FirstChild; child != null; child = child.NextSibling)
                CheckRange(documentText, child);
        }
    }
}
