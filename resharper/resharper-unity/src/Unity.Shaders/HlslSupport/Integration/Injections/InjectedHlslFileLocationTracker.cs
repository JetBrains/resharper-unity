#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections
{
    [SolutionComponent]
    public class InjectedHlslFileLocationTracker : CppFileLocationTrackerBase<InjectedHlslLocationInfo>
    {
        private readonly ISolution mySolution;
        private readonly CppExternalModule myCppExternalModule;
        private readonly CgIncludeDirectoryProvider myCgIncludeDirectoryProvider;

        public InjectedHlslFileLocationTracker(Lifetime lifetime, ISolution solution,
            IPersistentIndexManager persistentIndexManager, CppExternalModule cppExternalModule, CgIncludeDirectoryProvider cgIncludeDirectoryProvider)
            : base(lifetime, solution, persistentIndexManager, InjectedHlslLocationInfo.Read, InjectedHlslLocationInfo.Write)
        {
            mySolution = solution;
            myCppExternalModule = cppExternalModule;
            myCgIncludeDirectoryProvider = cgIncludeDirectoryProvider;
        }

        protected override CppFileLocation GetCppFileLocation(InjectedHlslLocationInfo t)
        {
            return t.ToCppFileLocation();
        }

        protected override bool IsApplicable(IPsiSourceFile sf) => sf.PrimaryPsiLanguage.Is<ShaderLabLanguage>();

        protected override HashSet<InjectedHlslLocationInfo> BuildData(IPsiSourceFile sourceFile)
        {
            var injections = InjectedHlslLocationHelper.GetCppFileLocations(sourceFile).Select(t =>
                new InjectedHlslLocationInfo(t.Location.Location, t.Location.RootRange, t.ProgramType));
            return new HashSet<InjectedHlslLocationInfo>(injections);
        }

        public bool Exists(CppFileLocation cppFileLocation)
        {
            return Exists(cppFileLocation.GetRandomSourceFile(mySolution), cppFileLocation);
        }

        protected override bool Exists(IPsiSourceFile sourceFile, CppFileLocation cppFileLocation)
        {
            if (Map.TryGetValue(sourceFile, out var result)
                && result.Any(d =>
                    d.FileSystemPath == cppFileLocation.Location && d.Range == cppFileLocation.RootRange))
                return true;
            return false;
        }

        private IEnumerable<CppFileLocation> GetIncludesLocation(IPsiSourceFile sourceFile, InjectedHlslProgramType type)
        {
            if (type == InjectedHlslProgramType.Unknown)
                return EnumerableCollection<CppFileLocation>.Empty;

            return Map[sourceFile]
                .Where(d => d.ProgramType == type)
                .Select(d => d.ToCppFileLocation());
        }

        private void AddImplicitCgIncludes(List<CppFileLocation> includeList, bool isSurface)
        {
            var cgIncludeFolder = myCgIncludeDirectoryProvider.GetCgIncludeFolderPath();
            if (cgIncludeFolder.ExistsDirectory)
            {
                var hlslSupport = cgIncludeFolder.Combine("HLSLSupport.cginc");
                if (hlslSupport.ExistsFile)
                {
                    includeList.Add(new CppFileLocation(myCppExternalModule, hlslSupport));
                }

                var variables = cgIncludeFolder.Combine("UnityShaderVariables.cginc");
                if (variables.ExistsFile)
                {
                    includeList.Add(new CppFileLocation(myCppExternalModule, variables));
                }

                var unityCg = cgIncludeFolder.Combine("UnityCG.cginc");
                if (unityCg.ExistsFile)
                {
                    includeList.Add(new CppFileLocation(myCppExternalModule, unityCg));
                }

                // from surface shader generated code
                if (isSurface)
                {
                    var lighting = cgIncludeFolder.Combine("Lighting.cginc");
                    if (lighting.ExistsFile)
                    {
                        includeList.Add(new CppFileLocation(myCppExternalModule, lighting));
                    }

                    var unityPbsLighting = cgIncludeFolder.Combine("UnityPBSLighting.cginc");
                    if (unityPbsLighting.ExistsFile)
                    {
                        includeList.Add(new CppFileLocation(myCppExternalModule, unityPbsLighting));
                    }

                    var autoLight = cgIncludeFolder.Combine("AutoLight.cginc");
                    if (autoLight.ExistsFile)
                    {
                        includeList.Add(new CppFileLocation(myCppExternalModule, autoLight));
                    }
                }
            }
        }

        public IEnumerable<CppFileLocation> GetIncludes(IPsiSourceFile sourceFile, ShaderProgramInfo shaderProgramInfo)
        {
            return GetIncludes(sourceFile, shaderProgramInfo.InjectedProgramType, shaderProgramInfo.ShaderType == ShaderType.Surface);
        }

        private IEnumerable<CppFileLocation> GetIncludes(IPsiSourceFile sourceFile, InjectedHlslProgramType type, bool isSurface)
        {
            var includeType = GetIncludeProgramType(type);

            var includeList = new List<CppFileLocation>
            {
                new(myCppExternalModule, mySolution.SolutionDirectory.Combine(Utils.ShaderConfigFile))
            };

            if (includeType != InjectedHlslProgramType.Unknown)
            {
                var includes = GetIncludesLocation(sourceFile, includeType);
                foreach (var include in includes)
                {
                    includeList.Add(include);
                }
            }

            if (type == InjectedHlslProgramType.CGProgram)
                AddImplicitCgIncludes(includeList, isSurface);

            return includeList;
        }

        private InjectedHlslProgramType GetIncludeProgramType(InjectedHlslProgramType injectedHlslProgramType)
        {
            switch (injectedHlslProgramType)
            {
                case InjectedHlslProgramType.CGProgram:
                    return InjectedHlslProgramType.CGInclude;
                case InjectedHlslProgramType.HLSLProgram:
                    return InjectedHlslProgramType.HLSLInclude;
                default:
                    return InjectedHlslProgramType.Unknown;
            }
        }
    }
}