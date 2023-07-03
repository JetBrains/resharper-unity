#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.LiveTemplates.Scope
{
    [ShellComponent]
    public class UnityShaderLabScopeProvider : ScopeProvider
    {
        public UnityShaderLabScopeProvider()
        {
            // Used when creating scope point from settings
            Creators.Add(TryToCreate<InUnityShaderLabFile>);
        }

        public override ITemplateScopePoint? CreateScope(Guid scopeGuid, string typeName, IEnumerable<Pair<string, string>> customProperties)
        {
            if (typeName == nameof(MustBeInShaderLabBlock))
            {
                if (customProperties.FirstOrNull(x => x.First == MustBeInShaderLabBlock.BlockKeywordAttributeName) is { } property
                    && MustBeInShaderLabBlock.TryCreateFromCommandKeyword(property.Second) is { } scope)
                {
                    scope.UID = scopeGuid;
                    return scope;
                }

                return null;
            }

            return base.CreateScope(scopeGuid, typeName, customProperties);
        }

        public override IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            var sourceFile = context.SourceFile;
            if (sourceFile == null)
                yield break;

            var caretOffset = context.CaretOffset;
            var prefix = LiveTemplatesManager.GetPrefix(caretOffset);

            var documentOffset = caretOffset - prefix.Length;
            if (!documentOffset.IsValid())
                yield break;

            var file = sourceFile.GetPsiFile<ShaderLabLanguage>(documentOffset);
            if (file == null || !file.Language.Is<ShaderLabLanguage>())
                yield break;

            yield return new InUnityShaderLabFile();
            var node = file.FindTokenAt(documentOffset);
            if (node is ITokenNode token && TryGetContainingBlockCommand(token) is { CommandKeyword: { } commandKeyword })
                yield return new MustBeInShaderLabBlock(commandKeyword.GetTokenType()); 
        }

        private IBlockCommand? TryGetContainingBlockCommand(ITokenNode node)
        {
            var enumerator = node.ContainingNodes().GetEnumerator();
            if (!MoveToBlockValue(enumerator, node))
                return null;
                
            return MoveToCommand(enumerator);

            static IBlockCommand? MoveToCommand(TreeNodeExtensions.ContainingNodeEnumerator enumerator)
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is IBlockCommand command)
                        return command;
                }

                return null;
            }

            static bool MoveToBlockValue(TreeNodeExtensions.ContainingNodeEnumerator enumerator, ITokenNode token)
            {
                while (enumerator.MoveNext())
                {
                    var containingNode = enumerator.Current;
                    if (containingNode is IBlockValue)
                    {
                        if (ShaderLabSyntax.CLike.TryGetSingleNonWhitespaceTokenOnLine(token, out var nonWhitespaceToken))
                        {
                            var tt = nonWhitespaceToken.GetTokenType();
                            return tt.IsIdentifier || tt.IsKeyword; // check if there just a single token on line either identifier or keyword 
                        }

                        return nonWhitespaceToken == null; // check for blank line as a valid case
                    }
                }
                
                return false;
            }
        }
    }
}