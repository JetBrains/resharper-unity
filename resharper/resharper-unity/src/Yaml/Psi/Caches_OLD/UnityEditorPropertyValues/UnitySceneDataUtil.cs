using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi.JavaScript.Util.Literals;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    internal static class UnitySceneDataUtil
    {
        private static StringSearcher ourStringSearcher = new StringSearcher("m_PersistentCalls:", true);
        public static void ExtractSimpleAndReferenceValues(IBuffer buffer, Dictionary<string, string> simpleValues, Dictionary<string, AssetDocumentReference> referenceValues, OneToSetMap<string, AssetDocumentReference> eventHandlerToScriptTarget)
        {
            // special field for accessing anchor id
            simpleValues["&anchor"] = GetAnchorFromBuffer(buffer);
            
            var lexer = new YamlLexer(buffer, true, false);
            lexer.Start();

            TokenNodeType currentToken;
            bool noColon = true;
            
            while ((currentToken = lexer.TokenType) != null)
            {
                if (noColon)
                {
                    if (currentToken == YamlTokenType.COLON)
                        noColon = false;

                    if (currentToken == YamlTokenType.NS_PLAIN_ONE_LINE_OUT)
                    {
                        var key = buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));
                      
                        // special filed for checking stripped documents
                        if (key.Equals("stripped"))
                            simpleValues["stripped"] = "1";
                    }
                }
                
                if (currentToken == YamlTokenType.INDENT)
                {
                    lexer.Advance();
                    currentToken = lexer.TokenType;
                    if (currentToken == YamlTokenType.NS_PLAIN_ONE_LINE_IN)
                    {
                        var key = buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));
                        
                        lexer.Advance();
                        SkipWhitespace(lexer);

                        currentToken = lexer.TokenType;
                        if (currentToken == YamlTokenType.COLON)
                        {
                            lexer.Advance();
                            SkipWhitespace(lexer);

                            currentToken = lexer.TokenType;
                            if (currentToken == YamlTokenType.LBRACE)
                            {
                                lexer.Advance();
                                var result = GetFileIdInner(buffer, lexer);
                                if (result != null)
                                    referenceValues[key] = result;
                            } else if (YamlTokenType.CHAMELEON_BLOCK_MAPPING_ENTRY_CONTENT_WITH_ANY_INDENT.Equals(currentToken))
                            {
                                var chameleonBuffer = ProjectedBuffer.Create(buffer, new TextRange(lexer.TokenStart, lexer.TokenEnd));
                                if (ourStringSearcher.Find(chameleonBuffer, 0, Math.Min(chameleonBuffer.Length, 150)) > 0)
                                {
                                    FillTargetAndMethod(chameleonBuffer, eventHandlerToScriptTarget);
                                }
                                else
                                {
                                    // sometimes, FileId is multiline
                                    var result = GetFileId(chameleonBuffer);
                                    if (result != null)
                                        referenceValues[key] = result;
                                }
                            }
                            else
                            {
                                var result = GetPrimitiveValue(buffer, lexer);
                                if (result != null)
                                    simpleValues[key] = result;
                            }
                        }
                    }
                    else
                    {
                        FindNextIndent(lexer);
                    }
                }
                else
                {
                    lexer.Advance();
                }
            }
        }

        public static string GetPrimitiveValue(IBuffer buffer, YamlLexer lexer)
        {
            SkipWhitespace(lexer);
            var token = lexer.TokenType;
            if (token == YamlTokenType.NS_PLAIN_ONE_LINE_IN || token == YamlTokenType.NS_PLAIN_ONE_LINE_OUT)
                return buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));

            if (token == YamlTokenType.NEW_LINE)
                return string.Empty;

            return null;
        }

        public static AssetDocumentReference GetFileId(IBuffer buffer, YamlLexer lexer)
        {
            SkipWhitespaceAndNewLine(lexer);
            if (lexer.TokenType == YamlTokenType.LBRACE)
            {
                lexer.Advance();
                return GetFileIdInner(buffer, lexer);
            }

            return null;
        }
        
        public static AssetDocumentReference GetFileId(IBuffer buffer)
        {
            var lexer = new YamlLexer(buffer, false, false);
            lexer.Start();
            
            if (lexer.TokenType == YamlTokenType.INDENT)
                lexer.Advance();
            
            SkipWhitespaceAndNewLine(lexer);
            if (lexer.TokenType == YamlTokenType.LBRACE)
            {
                lexer.Advance();
                return GetFileIdInner(buffer, lexer);
            }

            return null;
        }

        public static AssetDocumentReference GetFileIdInner(IBuffer buffer, YamlLexer lexer)
        {
            var fileId = GetFieldValue(buffer, lexer, "fileID");
            if (fileId == null)
                return null;
            
            SkipWhitespace(lexer);
            if (lexer.TokenType != YamlTokenType.COMMA)
                return new AssetDocumentReference(null, fileId);
            lexer.Advance();

            var guid = GetFieldValue(buffer, lexer, "guid");
            
            return new AssetDocumentReference(guid, fileId);
        }


        private static string GetFieldValue(IBuffer buffer, YamlLexer lexer, string name)
        {
            SkipWhitespaceAndNewLine(lexer);
            var currentToken = lexer.TokenType;
            if (currentToken == YamlTokenType.NS_PLAIN_ONE_LINE_IN)
            {
                var text = buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));
                if (!text.Equals(name))
                    return null;
            }
            lexer.Advance();
            SkipWhitespaceAndNewLine(lexer);

            currentToken = lexer.TokenType;
            if (currentToken != YamlTokenType.COLON)
                return null;
            
            lexer.Advance();
            SkipWhitespaceAndNewLine(lexer);
            
            currentToken = lexer.TokenType;
            if (currentToken == YamlTokenType.NS_PLAIN_ONE_LINE_IN)
            {
                var text = buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));
                lexer.Advance();
                return text;
            }

            return null;
        }

        private static void FillTargetAndMethod(IBuffer buffer, OneToSetMap<string, AssetDocumentReference> eventHandlerToScriptTarget)
        {
            var lexer = new YamlLexer(buffer, false, false);
            lexer.Start();
            
            
            TokenNodeType currentToken;

            AssetDocumentReference currentTarget = null;
            string currentMethod = null;
            
            while ((currentToken = lexer.TokenType) != null)
            {
                if (currentToken == YamlTokenType.NS_PLAIN_ONE_LINE_IN)
                {
                    var text = buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));
                    lexer.Advance();
                    SkipWhitespace(lexer);
                    currentToken = lexer.TokenType;
                    if (currentToken == YamlTokenType.COLON)
                    {
                        if (text.Equals("m_Target"))
                        {
                            if (currentMethod != null && currentTarget != null)
                                eventHandlerToScriptTarget.Add(currentMethod, currentTarget);

                            lexer.Advance();
                            currentTarget = GetFileId(buffer, lexer);
                        }
                        else if (text.Equals("m_MethodName"))
                        {
                            Assertion.Assert(currentTarget != null, "currentTarget != null");
                            lexer.Advance();
                            currentMethod = GetPrimitiveValue(buffer, lexer);
                        }
                    }
                }
                lexer.Advance();

            }
            
            if (currentMethod != null && currentTarget != null)
                eventHandlerToScriptTarget.Add(currentMethod, currentTarget);
        }
        
        public static void SkipWhitespace(YamlLexer lexer)
        {
            while (true)
            {
                var tokenType = lexer.TokenType;
                if (tokenType == null || tokenType != YamlTokenType.WHITESPACE)
                    return;
                lexer.Advance();
            }
        }
        
        private static void SkipWhitespaceAndNewLine(YamlLexer lexer)
        {
            while (true)
            {
                var tokenType = lexer.TokenType;
                if (tokenType == null || tokenType != YamlTokenType.WHITESPACE && tokenType != YamlTokenType.NEW_LINE)
                    return;
                lexer.Advance();
            }
        }
        

        public static bool FindNextIndent(YamlLexer lexer)
        {
            while (true)
            {
                var tokenType = lexer.TokenType;
                if (tokenType == null)
                    return false;
                if (tokenType == YamlTokenType.INDENT)
                    return true;
                lexer.Advance();
            }
        }
        
        public static string GetAnchorFromBuffer(IBuffer buffer)
        {
            var index = 0;
            while (true)
            {
                if (index == buffer.Length)
                    return null;
                
                if (buffer[index] == '&')
                    break;

                index++;
            }
            index++;

            var sb = new StringBuilder();
            while (index != buffer.Length && buffer[index].IsDigit())
            {
                sb.Append(buffer[index++]);
            }

            return sb.ToString();
        }
    }
}