using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.Yaml;
using JetBrains.ReSharper.Psi;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CSharp.Feature.Services.Bulbs
{
    public class ShowUsagesInUnityBulbAction : BulbActionBase
    {
        private readonly IDeclaredElement myDeclaredElement;
        private readonly AssetSerializationMode myAssetSerializationMode;
        private readonly UnityEditorFindUsageResultCreator myCreator;
        [NotNull] private readonly ConnectionTracker myTracker;

        public ShowUsagesInUnityBulbAction([NotNull] IDeclaredElement method, AssetSerializationMode assetSerializationMode,
            [NotNull] UnityEditorFindUsageResultCreator creator, [NotNull] ConnectionTracker tracker)
        {
            myDeclaredElement = method;
            myAssetSerializationMode = assetSerializationMode;
            myCreator = creator;
            myTracker = tracker;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution,
            IProgressIndicator progress)
        {
            
            if (!myAssetSerializationMode.IsForceText)
            {
                return textControl => ShowTooltip(textControl, "Feature is unavailable when the Unity asset serialisation mode is not set to 'Force Text'");
            }
            
            if (!myTracker.IsConnectionEstablished())
            {
                return textControl => ShowTooltip(textControl, "Unity is not running");
            }

            
            
            myCreator.CreateRequestToUnity(myDeclaredElement, null, true);
            return null;
        }

        public override string Text => "Show usages in Unity";
        
        public static bool IsAvailableFor(IDeclaredElement declaredElement, UnityApi api)
        {
            if (declaredElement == null)
                return false;

            if (declaredElement is IClass type && !api.IsUnityECSType(type))
                return true;

            return false;
        }
    }
}