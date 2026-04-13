using System.IO;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Projects;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.SerializeReference
{
    [TestFixture, ReuseSolution(false)]
    public class ElementIdCollisionTest : BaseTestWithExistingSolution
    {
        protected override string RelativeTestDataPath => "SerializationReference";

        // Verifies that the SerializeReference analysis pipeline handles ElementId hash collisions
        // gracefully (via TryAdd) instead of crashing with ArgumentException on Dictionary.Add.
        // RIDER-135672
        [Test]
        public void GenericClassesWithCollidingElementIds()
        {
            CollidingElementIdProviderMock.ForceCollisions = true;
            try
            {
                UnitySerializeReferenceProviderDescriptionInfo.CreateLifetimeCookie(TestLifetime);
                DoTestSolution(GetTestDataFilePath2(@"Solutions\GenericClassesLib01\GenericClassesLib01.sln"),
                    (_, solution) =>
                    {
                        var swea = SolutionAnalysisService.GetInstance(Solution);
                        using (TestPresentationMap.Cookie())
                        using (swea.RunAnalysisCookie())
                        {
                            swea.ReanalyzeAll();
                            var files = swea.GetFilesToAnalyze().OrderBy(f => f.Name).ToList();
                            foreach (var file in files)
                                swea.AnalyzeInvisibleFile(file);

                            var provider = solution.GetComponent<IUnitySerializedReferenceProvider>();
                            using var writer = new StringWriter();
                            provider.DumpFull(writer, solution);
                            var output = writer.ToString();
                            Assert.That(output, Is.Not.Empty,
                                "Provider index should not be empty after analysis with colliding ElementIds");
                        }
                    });
            }
            finally
            {
                CollidingElementIdProviderMock.ForceCollisions = false;
            }
        }
    }
}
