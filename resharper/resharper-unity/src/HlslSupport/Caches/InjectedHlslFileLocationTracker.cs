using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Injections;
using JetBrains.ReSharper.Plugins.Unity.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Caches
{
    [SolutionComponent]
    public class InjectedHlslFileLocationTracker : CppFileLocationTrackerBase<InjectedHlslLocationInfo>
    {
        private readonly ISolution mySolution;
        private readonly UnityVersion myUnityVersion;
        private readonly CppExternalModule myCppExternalModule;

        public InjectedHlslFileLocationTracker(Lifetime lifetime, ISolution solution, UnityVersion unityVersion,
            IPersistentIndexManager persistentIndexManager, CppExternalModule cppExternalModule)
            : base(
                lifetime, solution, persistentIndexManager, InjectedHlslLocationInfo.Read, InjectedHlslLocationInfo.Write)
        {
            mySolution = solution;
            myUnityVersion = unityVersion;
            myCppExternalModule = cppExternalModule;
        }

        protected override CppFileLocation GetCppFileLocation(InjectedHlslLocationInfo t)
        {
            return t.ToCppFileLocation();
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return sf.PrimaryPsiLanguage.Is<ShaderLabLanguage>();
        }

        protected override HashSet<InjectedHlslLocationInfo> BuildData(IPsiSourceFile sourceFile)
        {
            var injections = InjectedHlslLocationHelper.GetCppFileLocations(sourceFile).Select(t =>
                new InjectedHlslLocationInfo(t.Location.Location, t.Location.RootRange, t.ProgramType));
            return new HashSet<InjectedHlslLocationInfo>(injections);
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
            if (type == InjectedHlslProgramType.Uknown)
                return EnumerableCollection<CppFileLocation>.Empty;
            
            return Map[sourceFile]
                .Where(d => d.ProgramType == type)
                .Select(d => d.ToCppFileLocation());
        }

        public IEnumerable<CppFileLocation> GetIncludes(CppFileLocation cppFileLocation)
        {
            // PSI is not commited here
            // TODO: cpp global cache should calculate cache only when PSI for file with cpp injects is committed.
            
            var sourceFile = cppFileLocation.GetRandomSourceFile(mySolution);
            var range = cppFileLocation.RootRange;
            Assertion.Assert(range.IsValid, "range.IsValid");

            var buffer = sourceFile.Document.Buffer;
            var type = GetShaderProgramType(buffer, range.StartOffset);
            var includeType = GetIncludeProgramType(type);

            if (includeType != InjectedHlslProgramType.Uknown)
            {
                var includes = GetIncludesLocation(sourceFile, includeType);
                foreach (var include in includes)
                {
                    yield return include;
                }
            }

            var cgIncludeFolder = CgIncludeDirectoryTracker.GetCgIncludeFolderPath(myUnityVersion);        
            if (!cgIncludeFolder.ExistsDirectory)
                yield break;

            if (type == InjectedHlslProgramType.CGProgram)
            {
                var hlslSupport = cgIncludeFolder.Combine("HLSLSupport.cginc");
                if (hlslSupport.ExistsFile)
                {
                    yield return new CppFileLocation(myCppExternalModule, hlslSupport);
                }
                
                var variables = cgIncludeFolder.Combine("UnityShaderVariables.cginc");
                if (variables.ExistsFile)
                {
                    yield return new CppFileLocation(myCppExternalModule, variables);
                }
            }

            var lexer = CppLexer.Create(ProjectedBuffer.Create(buffer, range));
            lexer.Advance();
            while (lexer.TokenType != null)
            {
                var tokenType = lexer.TokenType;
                if (tokenType is CppDirectiveTokenNodeType)
                {
                    lexer.Advance();
                    var context = lexer.GetTokenText().TrimStart();
                    if (context.StartsWith("surface"))
                    {
                        var unityCG = cgIncludeFolder.Combine("UnityCG.cginc");
                        if (unityCG.ExistsFile)
                        {
                            yield return new CppFileLocation(myCppExternalModule, unityCG);
                        }
                
                        var lighting = cgIncludeFolder.Combine("Lighting.cginc");
                        if (lighting.ExistsFile)
                        {
                            yield return new CppFileLocation(myCppExternalModule, lighting);
                        }
                        
                        var unityPbsLighting = cgIncludeFolder.Combine("UnityPBSLighting.cginc");
                        if (unityPbsLighting.ExistsFile)
                        {
                            yield return new CppFileLocation(myCppExternalModule, unityPbsLighting);
                        }
                
                        var autoLight = cgIncludeFolder.Combine("AutoLight.cginc");
                        if (autoLight.ExistsFile)
                        {
                            yield return new CppFileLocation(myCppExternalModule, autoLight);
                        }
                        break;
                    }
                    // TODO: optimization if we found vertex/fragment/geometry/hull/domain?
                }
                lexer.Advance();
            }
        }

        private InjectedHlslProgramType GetShaderProgramType(IBuffer buffer, int locationStartOffset)
        {
            Assertion.Assert(locationStartOffset < buffer.Length, "locationStartOffset < buffer.Length");
            if (locationStartOffset >= buffer.Length)
                return InjectedHlslProgramType.Uknown;
            
            int curPos = locationStartOffset - 1;
            while (curPos > 0)
            {
                if (buffer[curPos].IsLetterFast())
                    break;
                curPos--;
            }

            var endPos = curPos;
            while (curPos > 0)
            {
                if (!buffer[curPos].IsLetterFast())
                {
                    curPos++;
                    break;
                }

                curPos--;
            }

            var text = buffer.GetText(new TextRange(curPos, endPos + 1)); // +1, because open interval [a, b)

            switch (text)
            {
                case "CGPROGRAM":
                    return InjectedHlslProgramType.CGProgram;
                case "CGINCLUDE":
                    return InjectedHlslProgramType.CGInclude;
                case "GLSLPROGRAM":
                    return InjectedHlslProgramType.GLSLProgram;
                case "GLSLINCLUDE":
                    return InjectedHlslProgramType.GLSLInclude;
                case "HLSLPROGRAM":
                    return InjectedHlslProgramType.HLSLProgram;
                case "HLSLINCLUDE":
                    return InjectedHlslProgramType.HLSLInclude;
                default:
                    return InjectedHlslProgramType.Uknown;
            }
        }

        private InjectedHlslProgramType GetIncludeProgramType(InjectedHlslProgramType injectedHlslProgramType)
        {
            switch (injectedHlslProgramType)
            {
                case InjectedHlslProgramType.CGProgram:
                    return InjectedHlslProgramType.CGInclude;
                case InjectedHlslProgramType.GLSLProgram:
                    return InjectedHlslProgramType.GLSLInclude;
                case InjectedHlslProgramType.HLSLProgram:
                    return InjectedHlslProgramType.HLSLInclude;
                default:
                    return InjectedHlslProgramType.Uknown;
            }
        }
    }
}