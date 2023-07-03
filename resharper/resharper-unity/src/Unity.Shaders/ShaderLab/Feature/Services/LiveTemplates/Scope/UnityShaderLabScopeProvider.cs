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
            Creators.Add(TryToCreate<InShaderLabRoot>);
        }

        public override ITemplateScopePoint? CreateScope(Guid scopeGuid, string typeName, IEnumerable<Pair<string, string>> customProperties)
        {
            if (typeName == nameof(InShaderLabBlock))
            {
                if (customProperties.FirstOrNull(x => x.First == InShaderLabBlock.BlockKeywordAttributeName) is { } property
                    && InShaderLabBlock.TryCreateFromCommandKeyword(property.Second) is { } scope)
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
            if (file.FindTokenAt(documentOffset) is not ITokenNode token || !IsValidToken(token))
                yield break;
            var containingNodes = token.ContainingNodes();
            while (MoveToContainingCommand(ref containingNodes, out var isInsideBlock) is { } command)
            {
                if (isInsideBlock)
                {
                    if (command.CommandKeyword is { } keyword)
                        yield return new InShaderLabBlock(keyword.GetTokenType());
                    yield break;
                }
                // if we're in middle of other command then don't produce any scope 
                if (command.FindFirstTokenIn() != token)
                    yield break;
            }
            yield return new InShaderLabRoot();
        }

        private bool IsValidToken(ITokenNode token)
        {
            if (ShaderLabSyntax.CLike.TryGetSingleNonWhitespaceTokenOnLine(token, out var nonWhitespaceToken))
            {
                var tt = nonWhitespaceToken.GetTokenType();
                return tt.IsIdentifier || tt.IsKeyword; // check if there just a single token on line either identifier or keyword 
            }

            return nonWhitespaceToken == null; // check for blank line as a valid case
        }

        private IShaderLabCommand? MoveToContainingCommand(ref TreeNodeExtensions.ContainingNodeEnumerator containingNodes, out bool isInsideBlock)
        {
            isInsideBlock = false;
            while (containingNodes.MoveNext())
            {
                var containingNode = containingNodes.Current;
                if (containingNode is IShaderLabCommand command)
                    return command;
                if (containingNode is IBlockValue)
                    isInsideBlock = true;
            }
            return null;
        }
    }
}