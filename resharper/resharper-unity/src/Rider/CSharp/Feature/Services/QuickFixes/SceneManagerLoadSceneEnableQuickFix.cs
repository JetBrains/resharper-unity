using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using static JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityProjectSettingsUtils;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class SceneManagerLoadSceneEnableQuickFix : QuickFixBase
    {
        private readonly LoadSceneDisabledSceneNameWarning myWarning;

        public SceneManagerLoadSceneEnableQuickFix(LoadSceneDisabledSceneNameWarning warning)
        {
            myWarning = warning;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var sceneName = myWarning.SceneName;
            Assertion.Assert(sceneName != null, "sceneName != null");

            var unityModule = GetUnityModule(solution);
            Assertion.Assert(unityModule != null, "unityModule != null");

            var buildSettings = GetEditorBuildSettings(unityModule);

            // An empty array would be an IFlowSequenceNode with no elements. We've got a disabled scene, so we know we
            // must have an item, which will be serialised as a block sequence
            var scenes = GetSceneCollection<IBlockSequenceNode>(buildSettings.GetDominantPsiFile<YamlLanguage>() as IYamlFile);
            Assertion.Assert(scenes != null, "scene != null");
            foreach (var entry in scenes.Entries)
            {
                var scene = entry.Value as IBlockMappingNode;
                if (scene == null)
                    continue;

                var path = GetUnityScenePathRepresentation(scene.GetMapEntryPlainScalarText("path")
                    .NotNull("EditorBuildSettings.scenes[x].path"));
                var simple = path.Split('/').Last();
                var isEnabledNode = scene.GetMapEntryValue<IPlainScalarNode>("enabled")
                    .NotNull("EditorBuildSettings.scenes[x].enabled");
                var isEnabled = isEnabledNode.GetPlainScalarText()
                    .NotNull("isEnabledNode.GetPlainScalarText() != null")
                    .Equals("1");
                if (!isEnabled && (path.Equals(sceneName) || simple.Equals(sceneName)))
                {
                    using (WriteLockCookie.Create(myWarning.Argument.IsPhysical()))
                    {
                        var text = YamlTokenType.NS_PLAIN_ONE_LINE_IN.Create("1");
                        if (isEnabledNode.Text != null)
                            LowLevelModificationUtil.ReplaceChildRange(isEnabledNode.Text, isEnabledNode.Text, text);
                        else
                            LowLevelModificationUtil.AddChild(isEnabledNode, text);
                    }
                }

                solution.GetComponent<IDaemon>().Invalidate();
                solution.GetComponent<UnityRefresher>().StartRefresh(RefreshType.Normal);
            }

            return null;
        }

        public override string Text => "Enable scene in build settings";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            if (!myWarning.Argument.IsValid())
                return false;

            var unityModule = GetUnityModule(myWarning.Argument.GetSolution());
            if (unityModule == null)
                return false;

            return GetEditorBuildSettings(unityModule) != null;
        }
    }
}