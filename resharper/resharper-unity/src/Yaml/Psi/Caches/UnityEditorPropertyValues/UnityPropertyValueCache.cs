using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [PsiComponent]
    public class UnityPropertyValueCache : SimpleICache<OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>>
    {
        private readonly PropertyValueLocalCache myLocalCache = new PropertyValueLocalCache();

        private static readonly HashSet<string> ourIgnoredMonoBehaviourEntries = new HashSet<string>()
        {
            "m_ObjectHideFlags",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset",
            "m_PrefabAsset",
            "m_Enabled",
            "m_EditorHideFlags",
            "m_Script",
            UnityYamlConstants.NameProperty,
            "m_EditorClassIdentifier"
        };

        private readonly IContextBoundSettingsStore myContextBoundSettingStore;

        public UnityPropertyValueCache(ISolution solution, Lifetime lifetime, IPersistentIndexManager persistentIndexManager,
            ISettingsStore settings)
            : base(lifetime, persistentIndexManager, CreateMarshaller())
        {
            myContextBoundSettingStore = settings.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
        }


        private static IUnsafeMarshaller<OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>>
            CreateMarshaller()
        {
            return UnsafeMarshallers
                .GetOneToManyMapMarshaller<MonoBehaviourProperty,
                    MonoBehaviourPropertyValue,
                    IList<MonoBehaviourPropertyValue>,
                    OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>>
                (
                    new UniversalMarshaller<MonoBehaviourProperty>(MonoBehaviourProperty.ReadFrom,
                        MonoBehaviourProperty.WriteTo),
                    new UniversalMarshaller<MonoBehaviourPropertyValue>(
                        MonoBehaviourPropertyValueMarshaller.Read,
                        MonoBehaviourPropertyValueMarshaller.Write),
                    n => new OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>(n));
        }

        protected override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return myContextBoundSettingStore.GetValue((UnitySettings k) => k.EnableInspectorPropertiesEditor) && 
                   base.IsApplicable(sourceFile) &&
                   sourceFile.LanguageType.Is<UnityYamlProjectFileType>() &&
                   sourceFile.PsiModule is UnityExternalFilesPsiModule &&
                   sourceFile.IsAsset();
        }


        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            // If YAML parsing is disabled, this will return null
            var file = sourceFile.GetDominantPsiFile<UnityYamlLanguage>() as IYamlFile;
            if (file == null)
                return null;

            var unityPropertyValueCacheItem = new OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue>();
            foreach (var document in file.DocumentsEnumerable)
            {
                ProcessScriptDocumentsForProperties(document.GetTextAsBuffer(), unityPropertyValueCacheItem);
            }

            if (unityPropertyValueCacheItem.Count == 0)
                return null;

            return unityPropertyValueCacheItem;
        }

        private void ProcessScriptDocumentsForProperties(IBuffer buffer, OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue> unityPropertyValueCacheItem)
        {
            var anchor = UnityGameObjectNamesCache.GetAnchorFromBuffer(buffer);
            var simpleValues = new Dictionary<string, string>();
            var referenceValues = new Dictionary<string, FileID>();
            FillDataViaLexer(buffer, simpleValues, referenceValues);

            var guid = referenceValues.GetValueSafe("m_Script")?.guid;
            if (guid == null)
                return;

            var gameObject = referenceValues.GetValueSafe("m_GameObject")?.fileID;
            if (gameObject == null)
                return;

            foreach (var (fieldName, value) in simpleValues)
            {
                if (ourIgnoredMonoBehaviourEntries.Contains(fieldName))
                    continue;

                var property = new MonoBehaviourProperty(guid, fieldName);
                var propertyValue = new MonoBehaviourPrimitiveValue(value, anchor, gameObject);
                unityPropertyValueCacheItem.Add(property, propertyValue);
            }

            foreach (var (fieldName, value) in referenceValues)
            {
                if (ourIgnoredMonoBehaviourEntries.Contains(fieldName))
                    continue;

                var property = new MonoBehaviourProperty(guid, fieldName);
                var propertyValue = new MonoBehaviourReferenceValue(value, anchor, gameObject);
                unityPropertyValueCacheItem.Add(property, propertyValue);
            }
        }

        private void FillDataViaLexer(IBuffer buffer, Dictionary<string, string> simpleValues,
            Dictionary<string, FileID> referenceValues)
        {
            var lexer = new YamlLexer(buffer);
            lexer.Start();

            TokenNodeType currentToken;
            while ((currentToken = lexer.TokenType) != null)
            {
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
                                var result = GetFileId(buffer, lexer);
                                if (result != null)
                                    referenceValues[key] = result;
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
        
        private string GetPrimitiveValue(IBuffer buffer, YamlLexer lexer)
        {
            var token = lexer.TokenType;
            if (token == YamlTokenType.NS_PLAIN_ONE_LINE_IN || token == YamlTokenType.NS_PLAIN_ONE_LINE_OUT)
                return buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));

            if (token == YamlTokenType.NEW_LINE)
                return string.Empty;

            return null;
        }

        private FileID GetFileId(IBuffer buffer, YamlLexer lexer)
        {
            var fileId = GetFieldValue(buffer, lexer, "fileID");
            if (fileId == null)
                return null;
            
            SkipWhitespace(lexer);
            if (lexer.TokenType != YamlTokenType.COMMA)
                return new FileID(null, fileId);
            lexer.Advance();

            var guid = GetFieldValue(buffer, lexer, "guid");
            
            return new FileID(guid, fileId);
        }


        private string GetFieldValue(IBuffer buffer, YamlLexer lexer, string name)
        {
            SkipWhitespace(lexer);
            var currentToken = lexer.TokenType;
            if (currentToken == YamlTokenType.NS_PLAIN_ONE_LINE_IN)
            {
                var text = buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));
                if (!text.Equals(name))
                    return null;
            }
            lexer.Advance();
            SkipWhitespace(lexer);

            currentToken = lexer.TokenType;
            if (currentToken != YamlTokenType.COLON)
                return null;
            
            lexer.Advance();
            SkipWhitespace(lexer);
            
            currentToken = lexer.TokenType;
            if (currentToken == YamlTokenType.NS_PLAIN_ONE_LINE_IN)
            {
                var text = buffer.GetText(new TextRange(lexer.TokenStart, lexer.TokenEnd));
                lexer.Advance();
                return text;
            }

            return null;
        }

        private void SkipWhitespace(YamlLexer lexer)
        {
            while (true)
            {
                var tokenType = lexer.TokenType;
                if (tokenType == null || tokenType != YamlTokenType.WHITESPACE)
                    return;
                lexer.Advance();
            }
        }

        private void FindNextIndent(YamlLexer lexer)
        {
            while (true)
            {
                var tokenType = lexer.TokenType;
                if (tokenType == null || tokenType == YamlTokenType.INDENT)
                    return;
                lexer.Advance();
            }
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            base.Merge(sourceFile, builtPart);
            if (builtPart is OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue> cache)
            {
                AddToLocalCache(sourceFile, cache);
            }
        }

        public override void MergeLoaded(object data)
        {
            base.MergeLoaded(data);
            PopulateLocalCache();
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private void PopulateLocalCache()
        {
            foreach (var (file, cacheItems) in Map)
                AddToLocalCache(file, cacheItems);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile,
            OneToListMap<MonoBehaviourProperty, MonoBehaviourPropertyValue> cacheItems)
        {
            foreach (var (property, values) in cacheItems)
            {
                foreach (var value in values)
                {
                    myLocalCache.Add(property, new MonoBehaviourPropertyValueWithLocation(sourceFile, value));
                }
            }
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var cacheItems))
            {
                foreach (var (property, values) in cacheItems)
                {
                    foreach (var value in values)
                    {
                        myLocalCache.Remove(property, new MonoBehaviourPropertyValueWithLocation(sourceFile, value));
                    }
                }
            }
        }

        public IEnumerable<MonoBehaviourPropertyValueWithLocation> GetPropertyValues(string guid, string propertyName)
        {
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myLocalCache.GetPropertyValues(query);
        }
        
        public int GetValueCount(string guid, string propertyName, object value)
        {
            return myLocalCache.GetValueCount(new MonoBehaviourProperty(guid, propertyName), value);
        }

        public int GetPropertyValuesCount(string guid, string propertyName)
        {
            return myLocalCache.GetPropertyValuesCount(new MonoBehaviourProperty(guid, propertyName));
        }
        
        public int GetPropertyUniqueValuesCount(string guid, string propertyName)
        {
            return myLocalCache.GetPropertyUniqueValuesCount(new MonoBehaviourProperty(guid, propertyName));
        }
        
        public IEnumerable<object> GetUniqueValues(string guid, string propertyName)
        {
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myLocalCache.GetUniqueValues(query);
        }
        
        public IEnumerable<MonoBehaviourPropertyValueWithLocation> GetUniqueValuesWithLocation(string guid, string propertyName)
        {
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myLocalCache.GetUniqueValuesWithLocation(query);
        }
        
        public int GetFilesCountWithoutChanges(string guid, string propertyName, object value)
        {
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myLocalCache.GetFilesCountWithoutChanges(query, value);
        }

        public int GetFilesWithPropertyCount(string guid, string propertyName)
        {
            var query = new MonoBehaviourProperty(guid, propertyName);
            return myLocalCache.GetFilesWithPropertyCount(query);
        }
    }
}