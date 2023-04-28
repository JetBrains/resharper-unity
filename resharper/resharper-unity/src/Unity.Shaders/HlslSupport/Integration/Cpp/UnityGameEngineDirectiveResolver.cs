#nullable enable

using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Cpp.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [SolutionComponent]
    public class UnityGameEngineDirectiveResolver : IGameEngineIncludeDirectiveResolver, IGameEngineIncludeDirectiveProvider
    {
        private readonly ISolution mySolution;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly PackageManager myPackageManager;

        public bool Active => mySolutionTracker.IsUnityProject.HasTrueValue() || mySolutionTracker.HasUnityReference.HasTrueValue();

        public UnityGameEngineDirectiveResolver(ISolution solution,
                                                UnitySolutionTracker solutionTracker,
                                                PackageManager packageManager)
        {
            mySolution = solution;
            mySolutionTracker = solutionTracker;
            myPackageManager = packageManager;
        }

        public bool IsApplicable(CppInclusionContext context, string path) => path.StartsWith("Packages/") && Active;

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
            if (package?.PackageFolder is not { } transformedPath)
                return path;
            if (endPos < path.Length)
                // DON'T USE Combine because it removes trailing slashes! We should be sure tail of the path exactly same as before for proper basePath resolving. We don't use pure string concatenation for interning support purposes only.
                transformedPath = VirtualFileSystemPath.CreateByCanonicalPath($"{transformedPath.FullPath}{path[endPos..]}", transformedPath.InteractionContext); 
            return transformedPath.FullPath;
        }
        
        public IEnumerable<CppIncludePath> ProvideIncludePaths(in ICppFileReference reference)
        {
            var qualifier = reference.GetQualifierReference();
            if (qualifier == null || qualifier.GetName() != "Packages")
                return Enumerable.Empty<CppIncludePath>();

            var items = new List<CppIncludePath>();
            foreach (var (packageId, packageData) in myPackageManager.Packages)
            {
                if (packageData.PackageFolder is {} packageFolder)
                    items.Add(new(packageId, packageFolder, false));
            }

            return items;
        }
    }
}
