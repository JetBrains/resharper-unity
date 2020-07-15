using System;
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
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    [SolutionComponent]
    public class ShaderLabCppFileLocationTracker : CppFileLocationTrackerBase<ShaderLabInjectLocationInfo>
    {
        private readonly ISolution mySolution;
        private readonly UnityVersion myUnityVersion;
        private readonly CppExternalModule myCppExternalModule;

        public ShaderLabCppFileLocationTracker(Lifetime lifetime, ISolution solution, UnityVersion unityVersion,
            IPersistentIndexManager persistentIndexManager, CppExternalModule cppExternalModule)
            : base(
                lifetime, solution, persistentIndexManager, ShaderLabInjectLocationInfo.Read, ShaderLabInjectLocationInfo.Write)
        {
            mySolution = solution;
            myUnityVersion = unityVersion;
            myCppExternalModule = cppExternalModule;
        }

        protected override CppFileLocation GetCppFileLocation(ShaderLabInjectLocationInfo t)
        {
            return t.ToCppFileLocation();
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return sf.PrimaryPsiLanguage.Is<ShaderLabLanguage>();
        }

        protected override HashSet<ShaderLabInjectLocationInfo> BuildData(IPsiSourceFile sourceFile)
        {
            var injections = ShaderLabCppHelper.GetCppFileLocations(sourceFile).Select(t =>
                new ShaderLabInjectLocationInfo(t.Location.Location, t.Location.RootRange, t.ProgramType));
            return new HashSet<ShaderLabInjectLocationInfo>(injections);
        }

        protected override bool Exists(IPsiSourceFile sourceFile, CppFileLocation cppFileLocation)
        {
            if (Map.TryGetValue(sourceFile, out var result)
                && result.Any(d =>
                    d.FileSystemPath == cppFileLocation.Location && d.Range == cppFileLocation.RootRange))
                return true;
            return false;
        }

        private IEnumerable<CppFileLocation> GetIncludesLocation(IPsiSourceFile sourceFile, ShaderProgramType type)
        {
            if (type == ShaderProgramType.Uknown)
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

            if (includeType != ShaderProgramType.Uknown)
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

            if (type == ShaderProgramType.CGProgram)
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

        private ShaderProgramType GetShaderProgramType(IBuffer buffer, int locationStartOffset)
        {
            Assertion.Assert(locationStartOffset < buffer.Length, "locationStartOffset < buffer.Length");
            if (locationStartOffset >= buffer.Length)
                return ShaderProgramType.Uknown;
            
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
                    return ShaderProgramType.CGProgram;
                case "CGINCLUDE":
                    return ShaderProgramType.CGInclude;
                case "GLSLPROGRAM":
                    return ShaderProgramType.GLSLProgram;
                case "GLSLINCLUDE":
                    return ShaderProgramType.GLSLInclude;
                case "HLSLPROGRAM":
                    return ShaderProgramType.HLSLProgram;
                case "HLSLINCLUDE":
                    return ShaderProgramType.HLSLInclude;
                default:
                    return ShaderProgramType.Uknown;
            }
        }

        private ShaderProgramType GetIncludeProgramType(ShaderProgramType shaderProgramType)
        {
            switch (shaderProgramType)
            {
                case ShaderProgramType.CGProgram:
                    return ShaderProgramType.CGInclude;
                case ShaderProgramType.GLSLProgram:
                    return ShaderProgramType.GLSLInclude;
                case ShaderProgramType.HLSLProgram:
                    return ShaderProgramType.HLSLInclude;
                default:
                    return ShaderProgramType.Uknown;
            }
        }
    }
}