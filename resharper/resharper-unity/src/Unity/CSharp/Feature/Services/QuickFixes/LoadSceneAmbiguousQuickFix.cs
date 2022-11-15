using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class LoadSceneAmbiguousQuickFix : IQuickFix
    {
        private readonly LoadSceneAmbiguousSceneNameWarning myWarning;

        public LoadSceneAmbiguousQuickFix(LoadSceneAmbiguousSceneNameWarning warning)
        {
            myWarning = warning;
            
        }
        
        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var cache = myWarning.Argument.GetSolution().GetComponent<UnityProjectSettingsCache>();
            
            
            // TODO 
            // consider that we have two scenes at build settings
            // Assets/test.scene
            // Assets/samples/test.scene
            // For "SceneManager.LoadScene("test")" we will suggest to change for "samples/test"
            var scenes = cache.GetScenesFromBuildSettings().Where(t => t.EndsWith(myWarning.SceneName));
            foreach (var scene in scenes)
            {
                yield return new ChangeSceneAtArgumentAction(myWarning.Argument, scene).ToQuickFixIntention();
            }
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            if (!myWarning.Argument.IsValid())
                return false;
            
            var unityCache = myWarning.Argument.GetSolution().TryGetComponent<UnityProjectSettingsCache>();
            if (unityCache == null)
                return false;
            
            if (unityCache.GetScenesFromBuildSettings().Any(t => t.Equals(myWarning.SceneName)))
                return false;

            return true;
        }
        
        private class ChangeSceneAtArgumentAction : BulbActionBase
        {
            private readonly IArgument myArgument;
            private readonly string myNewSceneName;

            public ChangeSceneAtArgumentAction(IArgument argument, string newSceneName)
            {
                myArgument = argument;
                myNewSceneName = newSceneName;
            }
            
            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                myArgument.Expression.ReplaceBy(CSharpElementFactory.GetInstance(myArgument)
                    .CreateStringLiteralExpression(myNewSceneName));
                return null;
            }

            public override string Text => string.Format(Strings.ChangeSceneAtArgumentAction_Text_Change_scene_name_to___0__, myNewSceneName);
        }
    }
}