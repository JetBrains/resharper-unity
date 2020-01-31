using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Serialization;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    public class SceneHierarchy
    {
        private static readonly ILogger ourLogger = Logger.GetLogger<SceneHierarchy>();

        public readonly Dictionary<FileID, IUnityHierarchyElement> Elements = new Dictionary<FileID, IUnityHierarchyElement>();

        private readonly Dictionary<FileID, FileID> myGameObjectsTransforms = new Dictionary<FileID, FileID>();
            
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

        public void AddSceneHierarchyElement(Dictionary<string, string> simpleValues, Dictionary<string, FileID> referenceValues)
        {
            var anchor = simpleValues.GetValueSafe("&anchor");
            if (string.IsNullOrEmpty(anchor))
                return;
            

            var id = new FileID(null, anchor);
            if (Elements.ContainsKey(id))
                ourLogger.Verbose($"Id = {anchor.Substring(0, Math.Min(anchor.Length, 100))} is defined several times for different documents");
            
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
                
                Elements[id] = new TransformHierarchyElement(id, correspondingId,prefabInstanceId, isStripped, rootOrder, gameObject, father);

                if (Elements.TryGetValue(gameObject, out var element))
                {
                    var goElement = (element as GameObjectHierarchyElement).NotNull("goElement != null");
                    goElement.TransformId = id;
                }
                else
                {
                    myGameObjectsTransforms[gameObject] = id;
                }
            }
            else if (referenceValues.ContainsKey(UnityYamlConstants.GameObjectProperty))
            {
                // component
                var gameObject = referenceValues.GetValueSafe(UnityYamlConstants.GameObjectProperty);
                Elements[id] = new ComponentHierarchyElement(id, correspondingId, prefabInstanceId, gameObject, isStripped);
            }
            else
            {
                // gameobject
                var transformId = myGameObjectsTransforms.GetValueSafe(id);
                if (transformId != null)
                    myGameObjectsTransforms.Remove(transformId);
                
                Elements[id] = new GameObjectHierarchyElement(id, correspondingId, prefabInstanceId, isStripped, transformId,
                    simpleValues.GetValueSafe(UnityYamlConstants.NameProperty));
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
                   referenceValues.GetValueSafe(UnityYamlConstants.PrefabInstanceProperty2017);
        }

        public void AddPrefabModification(IBuffer buffer)
        {
            
            var anchor = UnitySceneDataUtil.GetAnchorFromBuffer(buffer);
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
                            UnitySceneDataUtil.SkipWhitespace(lexer);
                            currentToken = lexer.TokenType;
                            
                            if (currentToken == YamlTokenType.COLON)
                            {
                                lexer.Advance();
                                
                                var result = UnitySceneDataUtil.GetFileId(buffer, lexer);
                                if (result != null)
                                    transformParentId = result;
                            }                            
                        } else if (text.Equals(UnityYamlConstants.ModificationsProperty))
                        {
                            var names = new Dictionary<FileID, string>();
                            var rootIndexes = new Dictionary<FileID, int?>();
                            GetModifications(buffer, lexer, indentSize, names, rootIndexes);
                            var id = new FileID(null, anchor);
                            Elements[id] = new ModificationHierarchyElement(id, null, null,  false, transformParentId, rootIndexes, names);
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

        /// <summary>
        /// This method skips m_Modifications entry in prefab document and stores modifications in dictionaries
        /// 
        /// After this method is executed, lexer current token is null or indent of next entry (after m_Modifications)
        /// </summary>
        private void GetModifications(IBuffer buffer, YamlLexer lexer, int parentIndentSize, Dictionary<FileID, string> names, Dictionary<FileID, int?> rootIndexes)
        {
            FileID curTarget = null;
            string curPropertyPath = null;
            string curValue = null;
            
            // Each property modifications is flow node:
            // - target: ..
            //   propertyPath: ..
            //   value:
            // Minus token means that new modification description is started
            // There are several entries in description. We are interested only
            // in target, propertyPath and value
            
            while (UnitySceneDataUtil.FindNextIndent(lexer))
            {
                var currentSize = lexer.TokenEnd - lexer.TokenStart;
                
                lexer.Advance();
                var tokenType = lexer.TokenType;
                
                
                if (tokenType == YamlTokenType.MINUS)
                {
                    currentSize++;
                    AddData();
                    lexer.Advance();
                }
                
                
                if (currentSize <= parentIndentSize)
                    break;
                
                UnitySceneDataUtil.SkipWhitespace(lexer);
                tokenType = lexer.TokenType;
                
                if (tokenType == YamlTokenType.NS_PLAIN_ONE_LINE_IN)
                {
                    var text = buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));
                    if (text.Equals(UnityYamlConstants.TargetProperty))
                    {
                        lexer.Advance();
                        UnitySceneDataUtil.SkipWhitespace(lexer);
                        lexer.Advance(); // skip column
                        
                        // [TODO] handle prefab in prefab
                        curTarget = UnitySceneDataUtil.GetFileId(buffer, lexer).WithGuid(null);
                    }
                    else if (text.Equals(UnityYamlConstants.PropertyPathProperty))
                    {
                        lexer.Advance();
                        UnitySceneDataUtil.SkipWhitespace(lexer);
                        lexer.Advance();
                        
                        curPropertyPath = UnitySceneDataUtil.GetPrimitiveValue(buffer, lexer);
                    }
                    else if (text.Equals(UnityYamlConstants.ValueProperty))
                    {
                        lexer.Advance();
                        UnitySceneDataUtil.SkipWhitespace(lexer);
                        lexer.Advance();
                        
                        curValue = UnitySceneDataUtil.GetPrimitiveValue(buffer, lexer);
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