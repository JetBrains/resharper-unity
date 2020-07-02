using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Injections;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    [SolutionComponent]
    public class ShaderLabCppFileLocationTracker : CppFileLocationTrackerBase<CppInjectionInfo>
    {
        private readonly ISolution mySolution;
        private readonly UnityVersion myUnityVersion;

        public ShaderLabCppFileLocationTracker(Lifetime lifetime, ISolution solution, UnityVersion unityVersion,
            IPersistentIndexManager persistentIndexManager)
            : base(
                lifetime, solution, persistentIndexManager, CppInjectionInfo.Read, CppInjectionInfo.Write)
        {
            mySolution = solution;
            myUnityVersion = unityVersion;
        }

        protected override CppFileLocation GetCppFileLocation(CppInjectionInfo t)
        {
            return t.ToCppFileLocation();
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return sf.PrimaryPsiLanguage.Is<ShaderLabLanguage>();
        }

        protected override HashSet<CppInjectionInfo> BuildData(IPsiSourceFile sourceFile)
        {
            var injections = ShaderLabCppHelper.GetCppFileLocations(sourceFile).Select(t =>
                new CppInjectionInfo(t.Location.Location, t.Location.RootRange, t.IsInclude));
            return new HashSet<CppInjectionInfo>(injections);
        }

        protected override bool Exists(IPsiSourceFile sourceFile, CppFileLocation cppFileLocation)
        {
            if (Map.TryGetValue(sourceFile, out var result)
                && result.Any(d =>
                    d.FileSystemPath == cppFileLocation.Location && d.Range == cppFileLocation.RootRange))
                return true;
            return false;
        }

        private CppFileLocation GetCgIncludeLocation(IPsiSourceFile sourceFile)
        {
            return Map[sourceFile]
                .Where(d => d.IsInclude)
                .Select(d => d.ToCppFileLocation())
                .FirstOrDefault(CppFileLocation.EMPTY);
        }

        public IEnumerable<CppFileLocation> GetIncludes(CppFileLocation cppFileLocation)
        {
            var sourceFile = cppFileLocation.GetRandomSourceFile(mySolution);
            var cgInclude = GetCgIncludeLocation(sourceFile);
            if (!cgInclude.Equals(CppFileLocation.EMPTY))
                yield return cgInclude;
    
            var cgIncludeFolder = CgIncludeDirectoryTracker.GetCgIncludeFolderPath(myUnityVersion);        
            if (!cgIncludeFolder.ExistsDirectory)
                yield break;
            
            var range = cppFileLocation.RootRange;
            Assertion.Assert(range.IsValid, "range.IsValid");
            var program = sourceFile.GetDominantPsiFile<ShaderLabLanguage>()?.GetContainingNodeAt<ICgContent>(new TreeOffset(range.StartOffset));
            Assertion.Assert(program != null, "program != null");
            
            if (program?.Parent?.FirstChild is ICgProgramBlock)
            {
                var hlslSupport = cgIncludeFolder.Combine("HLSLSupport.cginc");
                if (hlslSupport.ExistsFile)
                {
                    yield return new CppFileLocation(hlslSupport);
                }
                
                var variables = cgIncludeFolder.Combine("UnityShaderVariables.cginc");
                if (variables.ExistsFile)
                {
                    yield return new CppFileLocation(variables);
                }
            }
            
        }
    }
}