using System;
using System.Collections;
using System.Linq;
using JetBrains.ReSharper.FeaturesTestFramework.CodeStructure;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeStructure
{
    [RequireHlslSupport, TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabCodeStructureTest : PsiFileCodeStructureTestBase, IEnumerable
    {
        protected override string RelativeTestDataPath => @"ShaderLab\CodeStructure";
        
        [TestCaseSource(typeof(ShaderLabCodeStructureTest))]
        public void TestShaderFile(string shaderFileName) => DoOneTest(shaderFileName);

        public IEnumerator GetEnumerator() => TestDataPath.GetChildFiles("*.shader").Select(x => x.NameWithoutExtension).GetEnumerator();
    }
}
