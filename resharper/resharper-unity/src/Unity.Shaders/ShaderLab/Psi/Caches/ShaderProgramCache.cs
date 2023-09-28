#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
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
    public class ShaderProgramCache : SimplePsiSourceFileCacheWithLocalCache<ShaderProgramCache.Item, ImmutableArray<CppFileLocation>>
    {
        private readonly Dictionary<CppFileLocation, ShaderProgramInfo> myProgramInfos = new();
        
        public ShaderProgramCache(Lifetime lifetime, IShellLocks locks, IPersistentIndexManager persistentIndexManager) : base(lifetime, locks, persistentIndexManager, Item.Marshaller, "Unity::Shaders::ShaderProgramCacheUpdated")
        {
        }
        
        protected override bool IsApplicable(IPsiSourceFile sourceFile) => base.IsApplicable(sourceFile) && sourceFile.LanguageType.Is<ShaderLabProjectFileType>();
        
        public override object? Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (sourceFile.GetPrimaryPsiFile() is not ShaderLabFile shaderLabFile) return null;
            
            var entries = new LocalList<Item.Entry>();
            var buffer = shaderLabFile.Buffer ?? shaderLabFile.GetTextAsBuffer();
            foreach (var cgContent in shaderLabFile.Descendants<ICgContent>())
            {
                var content = cgContent.Content;
                var textRange = TextRange.FromLength(content.GetTreeStartOffset().Offset, content.GetTextLength());
                var programInfo = GetProgramInfo(new CppDocumentBuffer(buffer, textRange));
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
                myProgramInfos.Add(location, entry.ProgramInfo);
            }
            return builder.MoveToImmutable();
        }

        protected override void OnLocalRemoved(IPsiSourceFile sourceFile, ImmutableArray<CppFileLocation> removed)
        {
            foreach (var item in removed)
                myProgramInfos.Remove(item);
        }

        public bool TryGetShaderProgramInfo(CppFileLocation location, out ShaderProgramInfo shaderProgramInfo)
        {
            Locks.AssertReadAccessAllowed();
            return myProgramInfos.TryGetValue(location, out shaderProgramInfo);
        }

        private ShaderProgramInfo GetProgramInfo(CppDocumentBuffer buffer)
        {
            var isSurface = false;
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
            return new ShaderProgramInfo(definedMacroses, shaderTarget, isSurface);
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