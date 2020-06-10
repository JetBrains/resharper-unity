using System.Collections.Generic;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Injections;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    [SolutionComponent]
    public class ShaderLabCppFileLocationTracker : CppFileLocationTrackerBase<CppInjectionInfo>
    {
        public ShaderLabCppFileLocationTracker(Lifetime lifetime, ISolution solution,
            IPersistentIndexManager persistentIndexManager)
            : base(
                lifetime, solution, persistentIndexManager, CppInjectionInfo.Read, CppInjectionInfo.Write)
        {
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

        public CppFileLocation GetCgIncludeLocation(IPsiSourceFile sourceFile)
        {
            return Map[sourceFile]
                .Where(d => d.IsInclude)
                .Select(d => d.ToCppFileLocation())
                .FirstOrDefault(CppFileLocation.EMPTY);
        }
    }
}