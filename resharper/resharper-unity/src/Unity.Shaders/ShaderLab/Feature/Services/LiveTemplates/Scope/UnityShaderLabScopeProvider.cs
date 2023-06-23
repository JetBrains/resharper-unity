#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
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
            var node = file.FindNodeAt(documentOffset);
            if (node != null && TryGetContainingBlockCommand(node) is { CommandKeyword: { } commandKeyword })
                yield return new MustBeInShaderLabBlock(commandKeyword.GetTokenType()); 
        }

        private IBlockCommand? TryGetContainingBlockCommand(ITreeNode node)
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

            static bool MoveToBlockValue(TreeNodeExtensions.ContainingNodeEnumerator enumerator, ITreeNode node)
            {
                var prev = node;
                while (enumerator.MoveNext())
                {
                    var containingNode = enumerator.Current;
                    if (containingNode is IBlockValue)
                    {
                        // Either this node is directly inside of shaderBlock and is a whitespace on own line
                        if (prev == node && node.GetTokenType() == ShaderLabTokenType.WHITESPACE && node.PrevSibling?.NodeType == ShaderLabTokenType.NEW_LINE && node.NextSibling?.NodeType == ShaderLabTokenType.NEW_LINE)
                            return true;
                        return false;
                    }
                    prev = containingNode;
                }
                
                return false;
            }
        }
    }
}