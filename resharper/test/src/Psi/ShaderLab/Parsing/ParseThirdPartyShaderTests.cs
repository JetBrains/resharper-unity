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
    public class ParseBuiltinShadersTests : ThirdPartyShaderTests
    {
        // NOTE: Requires downloading builtin shaders from Unity, and copying to
        // test\data\psi\shaderLab\parsing\external\bultin_shaders-5.6.2f1
        protected override string ShaderFolderName => "builtin_shaders-5.6.2f1";
    }

    [Explicit]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public abstract class ThirdPartyShaderTests : BaseTestWithSingleProject
    {
        // NOTE: Requires downloading the built in shaders from Unity
        protected override string RelativeTestDataPath => @"psi\shaderLab\parsing\external\" + ShaderFolderName;
        protected abstract string ShaderFolderName { get; }

        [TestCaseSource(nameof(ThirdPartyShadersSource))]
        public void TestThirdPartyShaders(string name) => DoOneTest(name);

        public IEnumerable<string> ThirdPartyShadersSource
        {
            get
            {
                TestUtil.SetHomeDir(GetType().Assembly);

                var path = TestDataPath2;
                foreach (var file in path.GetChildFiles("*.shader", PathSearchFlags.RecurseIntoSubdirectories))
                    yield return file.MakeRelativeTo(path).ChangeExtension(string.Empty).FullPath;
            }
        }

        protected override void DoTest(IProject testProject)
        {
            var projectFile = testProject.GetAllProjectFiles().FirstNotNull();
            Assert.NotNull(projectFile);

            var text = projectFile.Location.ReadAllText2();
            var buffer = new StringBuffer(text.Text);

            var languageService = ShaderLabLanguage.Instance.LanguageService().NotNull();
            var lexer = languageService.GetPrimaryLexerFactory().CreateLexer(buffer);
            var psiModule = Solution.PsiModules().GetPrimaryPsiModule(testProject, TargetFrameworkId.Default);
            var parser = languageService.CreateParser(lexer, psiModule, null);
            var psiFile = parser.ParseFile().NotNull();

            if (DebugUtil.HasErrorElements(psiFile))
            {
                DebugUtil.DumpPsi(Console.Out, psiFile);
                Assert.Fail("File contains error elements");
            }

            Assert.AreEqual(text.Text, psiFile.GetText(), "Reconstructed text mismatch");
            CheckRange(text.Text, psiFile);
        }

        private static void CheckRange(string documentText, ITreeNode node)
        {
            Assert.AreEqual(node.GetText(), documentText.Substring(node.GetTreeStartOffset().Offset, node.GetTextLength()), "node range text mismatch");

            for (var child = node.FirstChild; child != null; child = child.NextSibling)
                CheckRange(documentText, child);
        }
    }
}