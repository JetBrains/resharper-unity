#nullable enable

using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [SolutionComponent]
    public class UnityGameEngineDirectiveResolver : IGameEngineIncludeDirectiveResolver
    {
        private readonly ISolution mySolution;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly PackageManager myPackageManager;

        public UnityGameEngineDirectiveResolver(ISolution solution,
                                                UnitySolutionTracker solutionTracker,
                                                PackageManager packageManager)
        {
            mySolution = solution;
            mySolutionTracker = solutionTracker;
            myPackageManager = packageManager;
        }

        public bool IsApplicable(CppInclusionContext context, string path)
        {
            return path.StartsWith("Packages/") &&
                   (mySolutionTracker.IsUnityProject.HasTrueValue() ||
                    mySolutionTracker.HasUnityReference.HasTrueValue());
        }

        public string TransformPath(CppInclusionContext context, string path)
        {
            var solutionFolder = mySolution.SolutionDirectory;
            if (solutionFolder.Combine(path).Exists != FileSystemPath.Existence.Missing)
                return path;

            var pos = path.IndexOf('/');
            if (pos == -1)
                return path;
            pos++;
            var endPos = path.IndexOf('/', pos);
            if (endPos == -1)
                endPos = path.Length;

            var packageName = path.Substring(pos, endPos - pos);
            var package = myPackageManager.GetPackageById(packageName);
            return package?.PackageFolder?.Combine(path[(endPos+1)..]).FullPath ?? path;
        }
    }
}
