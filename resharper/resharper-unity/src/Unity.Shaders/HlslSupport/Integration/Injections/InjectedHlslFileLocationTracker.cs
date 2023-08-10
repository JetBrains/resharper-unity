using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;
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
            : base(
                lifetime, solution, persistentIndexManager, InjectedHlslLocationInfo.Read, InjectedHlslLocationInfo.Write)
        {
            mySolution = solution;
            myCppExternalModule = cppExternalModule;
            myCgIncludeDirectoryProvider = cgIncludeDirectoryProvider;
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
            if (type == InjectedHlslProgramType.Uknown)
                return EnumerableCollection<CppFileLocation>.Empty;

            return Map[sourceFile]
                .Where(d => d.ProgramType == type)
                .Select(d => d.ToCppFileLocation());
        }

        public (IEnumerable<CppFileLocation> includeLocations, Dictionary<string, string> defines) GetProgramInfo(CppFileLocation cppFileLocation)
        {
            // PSI is not commited here
            // TODO: cpp global cache should calculate cache only when PSI for file with cpp injects is committed.

            var sourceFile = cppFileLocation.GetRandomSourceFile(mySolution);
            var range = cppFileLocation.RootRange;
            Assertion.Assert(range.IsValid, "range.IsValid");

            var buffer = sourceFile.Document.Buffer;
            var type = GetShaderProgramType(buffer, range.StartOffset);


            var defines = GetDefinedMacroses(ProjectedBuffer.Create(buffer, range), out var isSurface);
            var includes = GetIncludes(sourceFile, type, isSurface);

            return (includes, defines);
        }

        private Dictionary<string, string> GetDefinedMacroses(IBuffer buffer,out bool isSurface)
        {
            isSurface = false;
            var lexer = CppLexer.Create(buffer);
            lexer.Start();

            var definedMacroses = new Dictionary<string, string>();
            var shaderTarget = HlslConstants.SHADER_TARGET_25;
            while (lexer.TokenType != null)
            {
                var tokenType = lexer.TokenType;
                if (tokenType is CppDirectiveTokenNodeType)
                {
                    lexer.Advance();
                    var context = lexer.GetTokenText().TrimStart();
                    var (pragmaType, firstValue) = GetPragmaAndValue(context);
                    if (pragmaType.Equals("surface"))
                        isSurface = true;

                    // based on https://docs.unity3d.com/Manual/SL-ShaderPrograms.html
                    // We do not have any solution how we could handle multi_compile direcitves. It is complex task because
                    // a lot of different files could be produces from multi_compile combination
                    // Right now, we will consider first combination.
                    if (!firstValue.IsEmpty() && (pragmaType.Equals("multi_compile") || pragmaType.Equals("multi_compile_local") ||
                                                  pragmaType.Equals("shader_feature_local") || pragmaType.Equals("shader_feature")))
                    {
                        definedMacroses[firstValue] = "1";
                    }

                    if (pragmaType.Equals("target"))
                    {
                        var versionFromTarget = int.TryParse(firstValue.Replace(".", ""), out var result) ? result : HlslConstants.SHADER_TARGET_35;
                        shaderTarget = Math.Max(shaderTarget, versionFromTarget);
                    }

                    if (pragmaType.Equals("geometry"))
                        shaderTarget = Math.Max(shaderTarget, HlslConstants.SHADER_TARGET_40);

                    if (pragmaType.Equals("hull") || pragmaType.Equals("domain"))
                        shaderTarget = Math.Max(shaderTarget, HlslConstants.SHADER_TARGET_46);

                    // https://en.wikibooks.org/wiki/GLSL_Programming/Unity/Cookies
                    if (pragmaType.Equals("multi_compile_lightpass"))
                    {
                        // multi_compile_lightpass == multi_compile DIRECTIONAL, DIRECTIONAL_COOKIE, POINT, POINT_NOATT, POINT_COOKIE, SPOT
                        definedMacroses["DIRECTIONAL"] = "1";
                    }

                    // TODO: handle built-in https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html
                    // multi_compile_fwdbase, multi_compile_fwdadd, multi_compile_fwdadd_fullshadows, multi_compile_fog
                    // could not find information about that directives

                }
                lexer.Advance();
            }

            definedMacroses["SHADER_TARGET"] = shaderTarget.ToString();
            return definedMacroses;
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

                var unityCG = cgIncludeFolder.Combine("UnityCG.cginc");
                if (unityCG.ExistsFile)
                {
                    includeList.Add(new CppFileLocation(myCppExternalModule, unityCG));
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

        private IEnumerable<CppFileLocation> GetIncludes(IPsiSourceFile sourceFile, InjectedHlslProgramType type, bool isSurface)
        {
            var includeType = GetIncludeProgramType(type);

            var includeList = new List<CppFileLocation>();
            includeList.Add(new CppFileLocation(myCppExternalModule, mySolution.SolutionDirectory.Combine(Utils.ShaderConfigFile)));

            if (includeType != InjectedHlslProgramType.Uknown)
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

        private (string, string) GetPragmaAndValue(string context)
        {
            int i = 0;
            string GetIdentifier()
            {
                var sb = new StringBuilder();
                while (i < context.Length && char.IsWhiteSpace(context[i]))
                    i++;
                while (i < context.Length && !char.IsWhiteSpace(context[i]))
                {
                    sb.Append(context[i]);
                    i++;
                }

                return sb.ToString();
            }

            return (GetIdentifier(), GetIdentifier());
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
                case InjectedHlslProgramType.HLSLProgram:
                    return InjectedHlslProgramType.HLSLInclude;
                default:
                    return InjectedHlslProgramType.Uknown;
            }
        }
    }
}