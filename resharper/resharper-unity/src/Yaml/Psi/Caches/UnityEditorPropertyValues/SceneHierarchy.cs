using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Extension;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Web.WebConfig;
using JetBrains.Serialization;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    public class SceneHierarchy
    {
        public readonly Dictionary<FileID, IUnityHierarchyElement> Elements =new Dictionary<FileID, IUnityHierarchyElement>();

         private Dictionary<FileID, FileID> gameObjectsTransforms = new Dictionary<FileID, FileID>();
            
        public void WriteTo(UnsafeWriter writer)
        {
            writer.Write(Elements.Count);

            foreach (var (id, element) in Elements)
            {
                id.WriteTo(writer);
                writer.WritePolymorphic(element);
            }
        }

        public static SceneHierarchy Read(UnsafeReader reader)
        {
            var element = new SceneHierarchy();
            var count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var id = FileID.ReadFrom(reader);
                element.Elements.Add(id, reader.ReadPolymorphic<IUnityHierarchyElement>());
            }

            return element;
        }

        public void FillFrom(Dictionary<string, string> simpleValues, Dictionary<string, FileID> referenceValues)
        {
            var anchor = simpleValues.GetValueSafe("&anchor");
            if (anchor == null)
                return;

            var id = new FileID(null, anchor);
            var correspondingId = GetCorrespondingSourceObjectId(referenceValues);
            var prefabInstanceId = GetPrefabInstanceId(referenceValues);
            bool isStripped = simpleValues.ContainsKey("stripped");
            if (referenceValues.ContainsKey(UnityYamlConstants.FatherProperty))
            { 
                // transform component
                var rootOrder = int.TryParse(simpleValues.GetValueSafe(UnityYamlConstants.RootOrderProperty), out var r)
                    ? r
                    : -1;

                var gameObject = referenceValues.GetValueSafe(UnityYamlConstants.GameObjectProperty);
                var father = referenceValues.GetValueSafe(UnityYamlConstants.FatherProperty);
                
                Elements.Add(id, new TransformHierarchyElement(id, correspondingId, 
                    prefabInstanceId, isStripped, rootOrder, gameObject, father));

                if (Elements.TryGetValue(gameObject, out var element))
                {
                    var goElement = (element as GameObjectHierarchyElement).NotNull("goElement != null");
                    goElement.TransformId = id;
                }
                else
                {
                    gameObjectsTransforms[gameObject] = id;
                }
            }
            else if (referenceValues.ContainsKey(UnityYamlConstants.GameObjectProperty))
            {
                // component
                var gameObject = referenceValues.GetValueSafe(UnityYamlConstants.GameObjectProperty);
                Elements.Add(id, new ComponentHierarchyElement(id, correspondingId, prefabInstanceId, gameObject, isStripped));
            }
            else
            {
                // gameobject
                var transformId = gameObjectsTransforms.GetValueSafe(id);
                if (transformId != null)
                    gameObjectsTransforms.Remove(transformId);
                
                Elements.Add(id, new GameObjectHierarchyElement(id, correspondingId, prefabInstanceId, isStripped, transformId,
                    simpleValues.GetValueSafe(UnityYamlConstants.NameProperty)));
            }
        }

        private FileID GetCorrespondingSourceObjectId(Dictionary<string, FileID> referenceValues)
        {
            return referenceValues.GetValueSafe(UnityYamlConstants.CorrespondingSourceObjectProperty) ??
                   referenceValues.GetValueSafe(UnityYamlConstants.CorrespondingSourceObjectProperty2017);
        }
        
        private FileID GetPrefabInstanceId(Dictionary<string, FileID> referenceValues)
        {
            return referenceValues.GetValueSafe(UnityYamlConstants.PrefabInstanceProperty) ??
                   referenceValues.GetValueSafe(UnityYamlConstants.CorrespondingSourceObjectProperty2017);
        }

        public void FillFromPrefabModifications(IBuffer buffer)
        {
            var anchor = UnityGameObjectNamesCache.GetAnchorFromBuffer(buffer);
            if (anchor == null)
                return;
            
            var lexer = new YamlLexer(buffer, false, false);
            lexer.Start();

            TokenNodeType currentToken;

            var transformParentId = FileID.Null;
            
            while ((currentToken = lexer.TokenType) != null)
            {
                if (currentToken == YamlTokenType.INDENT)
                {
                    var indentSize = lexer.TokenEnd - lexer.TokenStart;
                    lexer.Advance();
                    currentToken = lexer.TokenType;

                    if (currentToken == YamlTokenType.NS_PLAIN_ONE_LINE_IN)
                    {
                        var text = buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));
                        if (text.Equals(UnityYamlConstants.TransformParentProperty))
                        {
                            lexer.Advance();
                            UnitySceneUtil.SkipWhitespace(lexer);
                            currentToken = lexer.TokenType;
                            
                            if (currentToken == YamlTokenType.COLON)
                            {
                                lexer.Advance();
                                
                                var result = UnitySceneUtil.GetFileId(buffer, lexer);
                                if (result != null)
                                    transformParentId = result;
                            }                            
                        } else if (text.Equals(UnityYamlConstants.ModificationsProperty))
                        {
                            var names = new Dictionary<FileID, string>();
                            var rootIndexes = new Dictionary<FileID, int?>();
                            GetModifications(buffer, lexer, indentSize, names, rootIndexes);
                            var id = new FileID(null, anchor);
                            Elements.Add(id,
                                new ModificationHierarchyElement(id, null, null,  false, transformParentId, rootIndexes, names));
                            return;
                        }
                    }
                }
                else
                {
                    lexer.Advance();
                }
            }
        }

        private void GetModifications(IBuffer buffer, YamlLexer lexer, int parentSize, Dictionary<FileID, string> names, Dictionary<FileID, int?> rootIndexes)
        {
            TokenNodeType tokenType = lexer.TokenType;
            
            FileID curTarget = null;
            string curPropertyPath = null;
            string curValue = null;
            
            while (UnitySceneUtil.FindNextIndent(lexer))
            {
                var currentSize = lexer.TokenEnd - lexer.TokenStart;
                
                lexer.Advance();
                tokenType = lexer.TokenType;
                if (tokenType == YamlTokenType.MINUS)
                {
                    currentSize++;
                    AddData();
                    lexer.Advance();
                }
                
                if (currentSize <= parentSize)
                    break;
                
                UnitySceneUtil.SkipWhitespace(lexer);
                tokenType = lexer.TokenType;
                
                if (tokenType == YamlTokenType.NS_PLAIN_ONE_LINE_IN)
                {
                    var text = buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));
                    if (text.Equals(UnityYamlConstants.TargetProperty))
                    {
                        lexer.Advance();
                        UnitySceneUtil.SkipWhitespace(lexer);
                        lexer.Advance(); // skip column
                        
                        // really null ?? TODO : check it
                        curTarget = UnitySceneUtil.GetFileId(buffer, lexer).WithGuid(null);
                    }
                    else if (text.Equals(UnityYamlConstants.PropertyPathProperty))
                    {
                        lexer.Advance();
                        UnitySceneUtil.SkipWhitespace(lexer);
                        lexer.Advance();
                        
                        curPropertyPath = UnitySceneUtil.GetPrimitiveValue(buffer, lexer);
                    }
                    else if (text.Equals(UnityYamlConstants.ValueProperty))
                    {
                        lexer.Advance();
                        UnitySceneUtil.SkipWhitespace(lexer);
                        lexer.Advance();
                        
                        curValue = UnitySceneUtil.GetPrimitiveValue(buffer, lexer);
                    }
                }
            }
            
            AddData();
            
            void AddData()
            {
                if (curTarget != null)
                {
                    if (curPropertyPath != null && curPropertyPath.Equals(UnityYamlConstants.NameProperty))
                    {
                        names[curTarget] = curValue;
                    } else if (curPropertyPath != null && curPropertyPath.Equals(UnityYamlConstants.RootOrderProperty))
                    {
                        rootIndexes[curTarget] = int.TryParse(curValue, out var r) ? (int?)r : null;
                    }
                        
                    curTarget = null;
                    curPropertyPath = null;
                    curValue = null;
                }
            }

        }
    }
}