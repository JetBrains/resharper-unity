#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Common.Utils;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches
{
    [PsiComponent]
    public class ShaderProgramCache : SimplePsiSourceFileCacheWithLocalCache<ShaderProgramCache.Item, VirtualFileSystemPath>, IBuildMergeParticipant<IPsiSourceFile>
    {
        // Multiple source files may have same virtual file system path and so may have clashing CppFileLocations which are path based. We have to count how many times path used and invalidate locations on any of source file change 
        private readonly Dictionary<VirtualFileSystemPath, (int Count, ImmutableArray<CppFileLocation> Locations)> myPathToLocations = new(); 
        private readonly Dictionary<CppFileLocation, ShaderProgramInfo> myProgramInfos = new();
        private readonly OneToSetMap<string, CppFileLocation> myShaderKeywords = new();

        private readonly UnityDialects myDialects;
        
        public ShaderProgramCache(Lifetime lifetime, IShellLocks locks, IPersistentIndexManager persistentIndexManager, UnityDialects dialects) : base(lifetime, locks, persistentIndexManager, Item.Marshaller, "Unity::Shaders::ShaderProgramCacheUpdated")
        {
            myDialects = dialects;
        }
        
        protected override bool IsApplicable(IPsiSourceFile sourceFile) => sourceFile.PrimaryPsiLanguage.Is<ShaderLabLanguage>();

        public object? Build(IPsiSourceFile sourceFile) => Build(sourceFile, false);

        public override object? Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (sourceFile.GetPrimaryPsiFile() is not ShaderLabFile shaderLabFile) return null;
            
            var entries = new LocalList<Item.Entry>();
            var buffer = shaderLabFile.Buffer ?? shaderLabFile.GetTextAsBuffer();
            foreach (var cgContent in shaderLabFile.Descendants<ICgContent>())
            {
                var content = cgContent.Content;
                var textRange = TextRange.FromLength(content.GetTreeStartOffset().Offset, content.GetTextLength());
                var programInfo = ReadProgramInfo(new CppDocumentBuffer(buffer, textRange), myDialects.ShaderLabHlslDialect);
                entries.Add(new Item.Entry(textRange, programInfo));
            }
            return new Item(entries.ToArray());
        }

        protected override VirtualFileSystemPath? BuildLocal(IPsiSourceFile sourceFile, Item persistent)
        {
            var path = sourceFile.GetLocation();
            if (path.IsEmpty)
                return null;

            if (myPathToLocations.TryGetValue(path, out var countAndLocations))
            {
                foreach (var location in countAndLocations.Locations)
                    RemoveProgramInfo(location);
                ++countAndLocations.Count;
            }
            else
                countAndLocations.Count = 1;
            
            var builder = ImmutableArray.CreateBuilder<CppFileLocation>(persistent.Entries.Length);
            foreach (var entry in persistent.Entries)
            {
                var location = new CppFileLocation(sourceFile, entry.Range);
                builder.Add(location);
                AddProgramInfo(location, entry.ProgramInfo);
            }
            countAndLocations.Locations = builder.MoveOrCopyToImmutableArray();

            myPathToLocations[path] = countAndLocations;
            return path;
        }

        protected override void OnLocalRemoved(IPsiSourceFile sourceFile, VirtualFileSystemPath path)
        {
            var countAndLocations = myPathToLocations[path];
            --countAndLocations.Count;
            if (countAndLocations.Count == 0)
            {
                foreach (var location in countAndLocations.Locations)
                    RemoveProgramInfo(location);
                myPathToLocations.Remove(path);
            }
            else
                myPathToLocations[path] = countAndLocations;
        }

        private void AddProgramInfo(CppFileLocation location, ShaderProgramInfo programInfo)
        {
            myProgramInfos.Add(location, programInfo);
            var shaderFeatures = programInfo.ShaderFeatures;
            if (shaderFeatures.IsEmpty) return;
            
            foreach (var shaderFeature in shaderFeatures)
            foreach (var entry in shaderFeature.Entries)
                myShaderKeywords.Add(entry.Keyword, location);
        }

        private void RemoveProgramInfo(CppFileLocation location)
        {
            if (myProgramInfos.Remove(location, out var programInfo) && programInfo.ShaderFeatures is {IsEmpty: false} shaderFeatures)
            {
                foreach (var shaderFeature in shaderFeatures)
                foreach (var entry in shaderFeature.Entries)
                    myShaderKeywords.Remove(entry.Keyword, location);
            }
        }

        public bool TryGetShaderProgramInfo(CppFileLocation location, [MaybeNullWhen(false)] out ShaderProgramInfo shaderProgramInfo)
        {
            Locks.AssertReadAccessAllowed();
            return myProgramInfos.TryGetValue(location, out shaderProgramInfo);
        }

        public bool TryGetShaderProgramInfo(VirtualFileSystemPath path, int offset, [MaybeNullWhen(false)] out ShaderProgramInfo shaderProgramInfo)
        {
            shaderProgramInfo = null;
            if (!myPathToLocations.TryGetValue(path, out var countAndLocations))
                return false;
            
            var result = countAndLocations.Locations.BinarySearchRo(offset, location => location.RootRange.StartOffset);
            var index = result.NearestIndexNotAboveTargetOrMinus1;
            if (index < 0)
                return false;
                
            var location = countAndLocations.Locations[index];
            if (offset >= location.RootRange.EndOffset)
                return false;

            shaderProgramInfo = myProgramInfos[location];
            return true;
        }

        public void ForEachKeyword(Action<string> action)
        {
            Locks.AssertReadAccessAllowed();
            foreach (var shaderKeyword in myShaderKeywords.Keys) 
                action(shaderKeyword);
        }

        public bool HasShaderKeyword(string keyword)
        {
            Locks.AssertReadAccessAllowed();
            return myShaderKeywords.ContainsKey(keyword);
        }
        
        public void ForEachLocation(Action<CppFileLocation> action) => ForEachLocation(new DelegateValueAction<CppFileLocation>(action));

        public void ForEachLocation<TAction>(TAction action) where TAction : IValueAction<CppFileLocation>
        {
            Locks.AssertReadAccessAllowed();
            foreach (var location in myProgramInfos.Keys) 
                action.Invoke(location);
        }

        public void ForEachKeywordLocation<TAction>(string keyword, ref TAction action) where TAction : IValueAction<CppFileLocation>
        {
            Locks.AssertReadAccessAllowed();
            foreach (var location in myShaderKeywords.GetReadOnlyValues(keyword)) 
                action.Invoke(location);
        }

        public void CollectLocationsTo(ICollection<CppFileLocation> target)
        {
            Locks.AssertReadAccessAllowed(); 
            foreach (var location in myProgramInfos.Keys) 
                target.Add(location);
        }

        public bool TryGetOrReadUpToDateProgramInfo(IPsiSourceFile sourceFile, CppFileLocation cppFileLocation, [MaybeNullWhen(false)] out ShaderProgramInfo shaderProgramInfo)
        {
            var range = cppFileLocation.RootRange;
            // Only injected shader programs supported for now
            if (!range.IsValid)
            {
                shaderProgramInfo = default;
                return false;
            }

            // PSI is not committed here
            // TODO: cpp global cache should calculate cache only when PSI for file with cpp injects is committed.
            if (!UpToDate(sourceFile))
                shaderProgramInfo = ReadProgramInfo(new CppDocumentBuffer(sourceFile.Document.Buffer, range), myDialects.ShaderLabHlslDialect);
            else if (!TryGetShaderProgramInfo(cppFileLocation, out shaderProgramInfo))
                return false;
            return true;
        }

        private ShaderProgramInfo ReadProgramInfo(CppDocumentBuffer buffer, UnityHlslDialectBase dialect)
        {
            var injectedProgramType = GetShaderProgramType(buffer);
            
            var lexer = CppLexer.Create(buffer);
            lexer.Start();

            var data = new ShaderProgramInfoData(lexer, dialect);
            return new ShaderProgramInfo(injectedProgramType, data.IsSurface ? ShaderType.Surface : ShaderType.VertFrag, data.ShaderTarget, data.ShaderFeatures.MoveOrCopyToImmutableArray(), data.DefinedMacros);
        }
        
        private InjectedHlslProgramType GetShaderProgramType(CppDocumentBuffer documentBuffer)
        {
            var locationStartOffset = documentBuffer.Range.StartOffset;
            var buffer = documentBuffer.Buffer; 
            Assertion.Assert(locationStartOffset < buffer.Length);
            if (locationStartOffset >= buffer.Length)
                return InjectedHlslProgramType.Unknown;

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
                    return InjectedHlslProgramType.Unknown;
            }
        }

        #region Cache item
        public class Item
        {
            public static readonly IUnsafeMarshaller<Item> Marshaller = new UniversalMarshaller<Item>(Read, Write);

            public readonly Entry[] Entries;  

            public Item(Entry[] entries)
            {
                Entries = entries;
            }
            
            private static readonly UnsafeReader.ReadDelegate<Entry> ourReadEntry = reader =>
            {
                var range = new TextRange(reader.ReadInt32(), reader.ReadInt32());
                var programInfo = ShaderProgramInfo.Marshaller.Unmarshal(reader);
                return new Entry(range, programInfo);
            };

            private static readonly UnsafeWriter.WriteDelegate<Entry> ourWriteEntry = static (writer, entry) =>
            {
                writer.WriteInt32(entry.Range.StartOffset);
                writer.WriteInt32(entry.Range.EndOffset);
                ShaderProgramInfo.Marshaller.Marshal(writer, entry.ProgramInfo);
            };

            private static Item Read(UnsafeReader reader) => new(reader.ReadArray(ourReadEntry) ?? Array.Empty<Entry>());

            private static void Write(UnsafeWriter writer, Item? item) => writer.WriteCollection(ourWriteEntry, item?.Entries);
            
            public readonly struct Entry
            {
                public readonly TextRange Range;
                public readonly ShaderProgramInfo ProgramInfo;

                public Entry(TextRange range, ShaderProgramInfo programInfo)
                {
                    Range = range;
                    ProgramInfo = programInfo;
                }
            }
        }
        #endregion

        #region ShaderProgramInfo data
        private struct ShaderProgramInfoData
        {
            private readonly IReadOnlyDictionary<string, PragmaCommand> myPragmas;
            private StringSplitter<SkipWhitespaceOrComment> myContentSplitter;
            private int myPragmaContentStartOffset;
            private static readonly Regex ourBackslashMergerRegex = new("\\\\\\s*\n", RegexOptions.Compiled);
            
            public bool IsSurface = false;
            public int ShaderTarget = HlslConstants.SHADER_TARGET_25;
            public readonly ImmutableArray<ShaderFeature>.Builder ShaderFeatures = ImmutableArray.CreateBuilder<ShaderFeature>();
            public readonly Dictionary<string, string> DefinedMacros = new();

            public ShaderProgramInfoData(CppLexer lexer, UnityHlslDialectBase dialect)
            {
                myPragmas = dialect.Pragmas;
                Read(lexer);
                DefinedMacros["SHADER_TARGET"] = ShaderTarget.ToString();
            }

            private void Read(CppLexer lexer)
            {
                while (lexer.TokenType != null)
                {
                    var tokenType = lexer.TokenType;
                    if (((CppTokenNodeType)tokenType).Kind() == CppTokenKind.PRAGMA_DIRECTIVE)
                    {
                        lexer.Advance();
                        
                        var pragmaContent = MergeBackslashSeparatedLines(lexer.GetTokenText());
                        myPragmaContentStartOffset = lexer.TokenStart;
                        myContentSplitter = new(pragmaContent, new SkipWhitespaceOrComment());
                        if (myContentSplitter.TryGetNextSliceAsString(out var pragmaType)) 
                            ReadPragmaCommand(pragmaType);
                    }
                    lexer.Advance();
                }
            }

            private string MergeBackslashSeparatedLines(string content) => ourBackslashMergerRegex.Replace(content, "");

            private void ReadPragmaCommand(string pragmaType)
            {
                switch (pragmaType)
                {
                    case "surface":
                        IsSurface = true;
                        break;
                    case "target":
                    {
                        if (myContentSplitter.TryGetNextSliceAsString(out var versionString))
                        {
                            var versionFromTarget = int.TryParse(versionString.Replace(".", ""), out var result) ? result : HlslConstants.SHADER_TARGET_35;
                            ShaderTarget = Math.Max(ShaderTarget, versionFromTarget);
                        }
                        break;
                    }
                    // https://en.wikibooks.org/wiki/GLSL_Programming/Unity/Cookies
                    case "multi_compile_lightpass":
                        // multi_compile_lightpass == multi_compile DIRECTIONAL, DIRECTIONAL_COOKIE, POINT, POINT_NOATT, POINT_COOKIE, SPOT
                        DefinedMacros["DIRECTIONAL"] = "1";
                        break;
                    // TODO: handle built-in https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html
                    // multi_compile_fwdbase, multi_compile_fwdadd, multi_compile_fwdadd_fullshadows, multi_compile_fog
                    // could not find information about that directives
                    default:
                    {
                        if (myPragmas.TryGetValue(pragmaType, out var pragmaCommand) && pragmaCommand is ShaderLabPragmaCommand { Info: var pragmaInfo })
                        {
                            if (TryReadShaderFeature(pragmaInfo, out var shaderFeature))
                                ShaderFeatures.Add(shaderFeature);
                            if (pragmaInfo.ImpliesShaderTarget > ShaderTarget)
                                ShaderTarget = pragmaInfo.ImpliesShaderTarget;
                        }
                        break;
                    }
                }
            }

            private bool IsNoKeywordMarker(StringSlice keyword)
            {
                var length = keyword.Length;
                for (var i = 0; i < length; ++i)
                {
                    if (keyword[i] != '_')
                        return false;
                }
                
                return true;
            }

            private bool TryReadShaderFeature(ShaderLabPragmaInfo pragmaInfo, out ShaderFeature shaderFeature)
            {
                if (pragmaInfo.ShaderFeatureType is ShaderFeatureType.KeywordList or ShaderFeatureType.KeywordListWithDisabledVariantForSingleKeyword)
                {
                    var allowDisableAllKeywords = false;
                    var entries = ImmutableArray.CreateBuilder<ShaderFeature.Entry>();
                    while (myContentSplitter.TryGetNextSlice(out var keyword, out var keywordOffset))
                    {
                        if (!IsNoKeywordMarker(keyword))
                            entries.Add(new ShaderFeature.Entry(keyword.ToString(), TextRange.FromLength(myPragmaContentStartOffset + keywordOffset, keyword.Length)));
                        else
                            allowDisableAllKeywords = true;
                    }

                    if (entries.Count > 0)
                    {
                        if (!allowDisableAllKeywords)
                            allowDisableAllKeywords = entries.Count == 1 && pragmaInfo.ShaderFeatureType == ShaderFeatureType.KeywordListWithDisabledVariantForSingleKeyword;
                        shaderFeature = new ShaderFeature(entries.MoveOrCopyToImmutableArray(), allowDisableAllKeywords);
                        return true;
                    }   
                }

                shaderFeature = default;
                return false;
            }
            
            private struct SkipWhitespaceOrComment : IValueFunction<StringSlice, int, int>
            {
                public int Invoke(StringSlice input, int position)
                {
                    var length = input.Length;
                    do
                    {
                        var ch = input[position];
                        if (ch == '/')
                        {
                            var nextPosition = position + 1;
                            if (nextPosition >= length)
                                return position;
                            
                            var nextCh = input[nextPosition];
                            if (nextCh == '/')
                                return length;
                            if (nextCh != '*')
                                return position;
                            position = input.IndexOf("*/", nextPosition + 1) is var index && index >= 0 ? index + 2 : length;
                        }
                        else if (char.IsWhiteSpace(ch))
                            ++position;
                        else
                            return position;
                    } while (position < length);

                    return length;
                }
            }
        }
        #endregion
    }
}