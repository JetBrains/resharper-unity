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
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
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
        private const string SHADER_VARIANT_NONE = "_";

        private static readonly HashSet<StringSlice> ourShaderFeatureDirectiveAllowingDisableAllKeywords = new() { "shader_feature", "shader_feature_local" };
        private static readonly HashSet<StringSlice> ourShaderFeatureDirectives = new() { "shader_feature", "shader_feature_local", "multi_compile", "multi_compile_local", "dynamic_branch", "dynamic_branch_local" };
        
        private readonly Dictionary<CppFileLocation, ShaderProgramInfo> myProgramInfos = new();
        private readonly OneToSetMap<string, CppFileLocation> myShaderVariants = new(); 
        
        public ShaderProgramCache(Lifetime lifetime, IShellLocks locks, IPersistentIndexManager persistentIndexManager) : base(lifetime, locks, persistentIndexManager, Item.Marshaller, "Unity::Shaders::ShaderProgramCacheUpdated")
        {
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
                var programInfo = ReadProgramInfo(new CppDocumentBuffer(buffer, textRange));
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
                myShaderVariants.Add(entry.Keyword, location);
        }

        private void RemoveProgramInfo(CppFileLocation location)
        {
            if (myProgramInfos.Remove(location, out var programInfo) && programInfo.ShaderFeatures is {IsEmpty: false} shaderFeatures)
            {
                foreach (var shaderFeature in shaderFeatures)
                foreach (var entry in shaderFeature.Entries)
                    myShaderVariants.Remove(entry.Keyword, location);
            }
        }

        public bool TryGetShaderProgramInfo(CppFileLocation location, [MaybeNullWhen(false)] out ShaderProgramInfo shaderProgramInfo)
        {
            Locks.AssertReadAccessAllowed();
            return myProgramInfos.TryGetValue(location, out shaderProgramInfo);
        }

        public void ForEachVariant(Action<string> action)
        {
            Locks.AssertReadAccessAllowed();
            foreach (var shaderVariant in myShaderVariants.Keys) 
                action(shaderVariant);
        }

        public void ForEachLocation<TAction>(string variant, ref TAction action) where TAction : IValueAction<CppFileLocation>
        {
            Locks.AssertReadAccessAllowed();
            foreach (var location in myShaderVariants.GetReadOnlyValues(variant)) 
                action.Invoke(location);
        }

        public ShaderProgramInfo GetOrReadUpToDateProgramInfo(IPsiSourceFile sourceFile, CppFileLocation cppFileLocation)
        {
            var range = cppFileLocation.RootRange;
            Assertion.Assert(range.IsValid);
            
            // PSI is not committed here
            // TODO: cpp global cache should calculate cache only when PSI for file with cpp injects is committed.
            ShaderProgramInfo? shaderProgramInfo;
            if (!UpToDate(sourceFile))
                shaderProgramInfo = ReadProgramInfo(new CppDocumentBuffer(sourceFile.Document.Buffer, range));
            else if (!TryGetShaderProgramInfo(cppFileLocation, out shaderProgramInfo)) 
                Assertion.Fail($"Shader program info is missing for {cppFileLocation}");

            return shaderProgramInfo;
        }

        private ShaderProgramInfo ReadProgramInfo(CppDocumentBuffer buffer)
        {
            var injectedProgramType = GetShaderProgramType(buffer);
            
            var isSurface = false;
            var lexer = CppLexer.Create(buffer);
            lexer.Start();

            var definedMacros = new Dictionary<string, string>();
            var shaderTarget = HlslConstants.SHADER_TARGET_25;
            var shaderFeatures = ImmutableArray.CreateBuilder<ShaderFeature>();
            while (lexer.TokenType != null)
            {
                var tokenType = lexer.TokenType;
                if (tokenType is CppDirectiveTokenNodeType)
                {
                    lexer.Advance();
                    var context = lexer.GetTokenText();

                    var contextStartOffset = lexer.TokenStart;
                    var slicer = StringSplitter.ByWhitespace(context);
                    slicer.TryGetNextSlice(out var pragmaType);
                    if (pragmaType.Equals("surface"))
                        isSurface = true;

                    if (TryReadShaderFeature(contextStartOffset, slicer, pragmaType, out var shaderFeature)) 
                        shaderFeatures.Add(shaderFeature);

                    if (pragmaType.Equals("target") && slicer.TryGetNextSlice(out var versionString))
                    {
                        var versionFromTarget = int.TryParse(versionString.ToString().Replace(".", ""), out var result) ? result : HlslConstants.SHADER_TARGET_35;
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
                        definedMacros["DIRECTIONAL"] = "1";
                    }

                    // TODO: handle built-in https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html
                    // multi_compile_fwdbase, multi_compile_fwdadd, multi_compile_fwdadd_fullshadows, multi_compile_fog
                    // could not find information about that directives

                }
                lexer.Advance();
            }

            definedMacros["SHADER_TARGET"] = shaderTarget.ToString();
            return new ShaderProgramInfo(injectedProgramType, isSurface ? ShaderType.Surface : ShaderType.VertFrag, shaderTarget, shaderFeatures.MoveOrCopyToImmutableArray(), definedMacros);
        }

        private static bool TryReadShaderFeature(int baseOffset, StringSplitter<CharPredicates.IsWhitespacePredicate> slicer, StringSlice pragmaType, out ShaderFeature shaderFeature)
        {
            if (ourShaderFeatureDirectives.Contains(pragmaType))
            {
                var allowDisableAllKeywords = false;
                var entries = ImmutableArray.CreateBuilder<ShaderFeature.Entry>();
                while (slicer.TryGetNextSlice(out var keyword, out var keywordOffset))
                {
                    if (!keyword.Equals(SHADER_VARIANT_NONE))
                        entries.Add(new ShaderFeature.Entry(keyword.ToString(), TextRange.FromLength(baseOffset + keywordOffset, keyword.Length)));
                    else
                        allowDisableAllKeywords = true;
                }

                if (entries.Count > 0)
                {
                    if (!allowDisableAllKeywords)
                        allowDisableAllKeywords = entries.Count == 1 && ourShaderFeatureDirectiveAllowingDisableAllKeywords.Contains(pragmaType);
                    shaderFeature = new ShaderFeature(entries.MoveOrCopyToImmutableArray(), allowDisableAllKeywords);
                    return true;
                }
            }

            shaderFeature = default;
            return false;
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
    }
}