using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Web.WebConfig;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.Util;
using static JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityProjectSettingsUtils;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class SceneManagerLoadSceneQuickFix : IQuickFix
    {
        private readonly LoadSceneUnknownSceneNameWarning myWarning;

        public SceneManagerLoadSceneQuickFix(LoadSceneUnknownSceneNameWarning warning)
        {
            myWarning = warning;
        }
        
        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var sceneName = myWarning.SceneName;
            Assertion.Assert(sceneName != null, "sceneName != null");

            var solution = myWarning.Argument.GetSolution();
            var unityModule = GetUnityModule(solution);
            Assertion.Assert(unityModule != null, "unityModule != null");

            var correspondingFiles = GetCorrespondingSourceFiles(sceneName, unityModule).ToArray();
            foreach (var (file, unityPath) in correspondingFiles)
            {
                yield return new LoadSceneFixBulbAction(myWarning.Argument, correspondingFiles.Length > 1, 
                    unityPath, solution.GetComponent<MetaFileGuidCache>().GetAssetGuid(file), unityModule).ToQuickFixIntention();
            }
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            if (!myWarning.Argument.IsValid())
                return false;
            
            return GetEditorBuildSettings(GetUnityModule(myWarning.Argument.GetSolution())) != null;
        }

        private IEnumerable<(IPsiSourceFile file, string unityPath)> GetCorrespondingSourceFiles(string scene, UnityExternalFilesPsiModule psiModule)
        {         
            var files = psiModule.SourceFiles;
            if (IsScenePath(scene))
            {
                var sceneName = scene.Split('/').Last();
                bool IsCorrespondingSourceFile(IPsiSourceFile psiSourceFile)
                {
                    if (!psiSourceFile.GetExtensionWithDot().Equals(UnityYamlFileExtensions.SceneFileExtensionWithDot))
                        return false;
                    var psiPath = psiSourceFile.GetLocation();
                    if (!psiPath.NameWithoutExtension.Equals(sceneName))
                        return false;
                    
                    return GetUnityPathFor(psiSourceFile).Equals(scene);
                }

                var file = files.FirstOrDefault(IsCorrespondingSourceFile);
                if (file == null)
                    return EmptyList<(IPsiSourceFile, string)>.Instance;
                return new[] {(file, GetUnityPathFor(file))};

            }

            return files.Where(t => t.Name.Equals(scene + UnityYamlFileExtensions.SceneFileExtensionWithDot))
                .Select(f => (f, GetUnityPathFor(f)));
        }

        private bool IsScenePath(string sceneName)
        {
            return sceneName.Contains("/");
        }

        private class LoadSceneFixBulbAction : BulbActionBase
        {
            private readonly IArgument myArgument;
            private readonly bool myReplaceArgument;
            private readonly string mySceneName;
            private readonly string myGuid;
            private readonly UnityExternalFilesPsiModule myUnityModule;

            public LoadSceneFixBulbAction(IArgument argument, bool replaceArgument, string sceneName,
                string guid, UnityExternalFilesPsiModule unityModule)
            {
                myArgument = argument;
                myReplaceArgument = replaceArgument;
                mySceneName = sceneName;
                myGuid = guid;
                myUnityModule = unityModule;
            }
            
            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var editorBuildSettings = GetEditorBuildSettings(myUnityModule);
                Assertion.Assert(editorBuildSettings != null, "editorBuildSettings != null");
                var yamlFile = editorBuildSettings.GetDominantPsiFile<YamlLanguage>() as IYamlFile;
                Assertion.Assert(yamlFile != null, "yamlFile != null");
                
                var scenesNode = GetSceneCollection(yamlFile);

                using (WriteLockCookie.Create(yamlFile.IsPhysical()))
                {
                    if (scenesNode is IFlowSequenceNode)
                    {
                        var blockSequenceNode = CreateBlockSequenceNode(mySceneName, myGuid, myUnityModule);
                        LowLevelModificationUtil.ReplaceChildRange(scenesNode, scenesNode, blockSequenceNode);
                    } else if (scenesNode is IBlockSequenceNode)
                    {
                        var blockSequenceNode = CreateBlockSequenceNode(mySceneName, myGuid, myUnityModule);
                        LowLevelModificationUtil.AddChild(scenesNode, YamlTokenType.INDENT.Create("  "));
                        LowLevelModificationUtil.AddChild(scenesNode, blockSequenceNode.Entries[0]);
                        LowLevelModificationUtil.AddChild(scenesNode, YamlTokenType.NEW_LINE.Create("\n"));
                    }
                }

                if (myReplaceArgument)
                    myArgument.Expression.ReplaceBy(CSharpElementFactory.GetInstance(myArgument)
                        .CreateStringLiteralExpression(mySceneName));

                solution.GetComponent<IDaemon>().Invalidate();
            
                solution.GetComponent<UnityRefresher>().StartRefresh(RefreshType.Normal);
                return null;
            }

            public override string Text => $"Add '{mySceneName}' to build settings";
            
            private IBlockSequenceNode CreateBlockSequenceNode(string sceneName, string guid, IPsiModule module)
            {
                // TODO yaml psi factory?
                var buffer = new StringBuffer($"EditorBuildSettings:\n  m_Scenes:\n  - enabled: 1\n    path: Assets/{sceneName}.unity\n    guid: {guid}");
                var languageService = YamlLanguage.Instance.LanguageService().NotNull();
                var lexer = languageService.GetPrimaryLexerFactory().CreateLexer(buffer);
                var file = (languageService.CreateParser(lexer, module, null) as IYamlParser)
                    .NotNull("Not yaml parser").ParseFile() as IYamlFile;

                var sceneRecord = GetSceneCollection((file.Documents.First().Body.BlockNode as IBlockMappingNode)
                    .NotNull("blockMappingNode != null")) as IBlockSequenceNode;
                SandBox.CreateSandBoxFor(sceneRecord.NotNull("sceneRecord != null"), module);
                return sceneRecord;
            }
        }
    }
}