#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    public class ShaderProgramCache : SimplePsiSourceFileCacheWithLocalCache<ShaderProgramCache.Item, ImmutableArray<CppFileLocation>>, IBuildMergeParticipant<IPsiSourceFile>
    {
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

        protected override ImmutableArray<CppFileLocation> BuildLocal(IPsiSourceFile sourceFile, Item persistent)
        {
            var builder = ImmutableArray.CreateBuilder<CppFileLocation>(persistent.Entries.Length);
            foreach (var entry in persistent.Entries)
            {
                var location = new CppFileLocation(sourceFile, entry.Range);
                builder.Add(location);
                AddProgramInfo(location, entry.ProgramInfo);
            }
            return builder.MoveToImmutable();
        }

        protected override void OnLocalRemoved(IPsiSourceFile sourceFile, ImmutableArray<CppFileLocation> removed)
        {
            foreach (var location in removed)
                RemoveProgramInfo(location);
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
            Assertion.Assert(range.IsValid);
            
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
            private const string SHADER_KEYWORD_NONE = "_";
            
            private readonly IReadOnlyDictionary<string, PragmaCommand> myPragmas;
            private StringSplitter<CharPredicates.IsWhitespacePredicate> myContentSplitter;
            private int myPragmaContentStartOffset;
            
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
                        
                        var pragmaContent = lexer.GetTokenText();
                        myPragmaContentStartOffset = lexer.TokenStart;
                        myContentSplitter = StringSplitter.ByWhitespace(pragmaContent);
                        if (myContentSplitter.TryGetNextSliceAsString(out var pragmaType)) 
                            ReadPragmaCommand(pragmaType);
                    }
                    lexer.Advance();
                }
            }

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

            private bool TryReadShaderFeature(ShaderLabPragmaInfo pragmaInfo, out ShaderFeature shaderFeature)
            {
                if (pragmaInfo.DeclaresKeywords)
                {
                    var allowDisableAllKeywords = false;
                    var entries = ImmutableArray.CreateBuilder<ShaderFeature.Entry>();
                    while (myContentSplitter.TryGetNextSlice(out var keyword, out var keywordOffset))
                    {
                        if (!keyword.Equals(SHADER_KEYWORD_NONE))
                            entries.Add(new ShaderFeature.Entry(keyword.ToString(), TextRange.FromLength(myPragmaContentStartOffset + keywordOffset, keyword.Length)));
                        else
                            allowDisableAllKeywords = true;
                    }

                    if (entries.Count > 0)
                    {
                        if (!allowDisableAllKeywords)
                            allowDisableAllKeywords = entries.Count == 1 && pragmaInfo.HasDisabledVariantForSingleKeyword;
                        shaderFeature = new ShaderFeature(entries.MoveOrCopyToImmutableArray(), allowDisableAllKeywords);
                        return true;
                    }   
                }

                shaderFeature = default;
                return false;
            }
        }
        #endregion
    }
}