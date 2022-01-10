using JetBrains.IDE.UI.Extensions;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;
using JetBrains.Rider.Model.UIAutomation;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public class UnityEventTargetRefactoringPage : SingleBeRefactoringPage
    {
        private readonly DeferredCacheController myDeferredCacheController;

        // Scenarios:
        // 1. Unity is not connected - show confirmation prompt
        // 2. Unity plugin is not installed - show confirmation + save prompt
        // 3. Unity is connected with unmodified scene(s) - show confirmation prompt (with checkbox)
        // 4. Unity is connected with modified scene(s) - show confirmation + save prompt and block until scenes are
        //    saved + documents committed
        //
        // We can't tell the difference between plugin not connected and not installed. Can't risk overwriting unsaved
        // open scenes, so we always show a confirmation prompt
        public UnityEventTargetRefactoringPage(Lifetime lifetime, DeferredCacheController deferredCacheController)
            : base(lifetime)
        {
            myDeferredCacheController = deferredCacheController;
        }

        public override string Title => "Rename Unity reference";
        public override string Description => myDeferredCacheController.IsProcessingFiles() ? "Asset index is not ready." : "Rename references in Unity asset files?";

        public override BeControl GetPageContent()
        {
            return BeControls.GetRichText(GetText(), wrap: true);
        }


        private string GetText()
        {
            if (myDeferredCacheController.IsProcessingFiles())
                return "Symbol will not be renamed in assets.";
            
            return "Please ensure the project is saved in the Unity Editor, or any changes will be lost.";
        }
    }
}