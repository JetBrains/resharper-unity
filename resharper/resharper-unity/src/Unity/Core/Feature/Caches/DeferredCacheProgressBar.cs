#nullable enable

using System;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost.Progress;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches
{
    [SolutionComponent]
    public class DeferredCacheProgressBar
    {
        private readonly IViewableProperty<IPsiSourceFile> myCurrentFile = new ViewableProperty<IPsiSourceFile>(null);
        private readonly Lifetime myLifetime;
        private readonly DeferredHelperCache myCache;
        private readonly IShellLocks myLocks;
        private readonly BackgroundProgressManager? myBackgroundProgressManager;

        public DeferredCacheProgressBar(Lifetime lifetime, DeferredHelperCache cache, IShellLocks locks,
            BackgroundProgressManager? backgroundProgressManager = null)
        {
            myLifetime = lifetime;
            myCache = cache;
            myLocks = locks;
            myBackgroundProgressManager = backgroundProgressManager;
        }

        public void Start(Lifetime startLifetime)
        {
            if (myBackgroundProgressManager == null)
                return;

            // avoid problems with background task host after terminating lifetime on daemon thread
            var lifetimeDef = myLifetime.CreateNested();
            startLifetime.OnTermination(() =>
            {
                myLocks.Tasks.StartNew(myLifetime, Scheduling.MainDispatcher, () => lifetimeDef.Terminate());
            });

            var count = myCache.FilesToProcess.Count;
            var processedCount = 0;
            var progress = new Property<double>("DeferredCacheProgressBarProgress", 0);

            myCache.AfterRemoveFromProcess.Advise(startLifetime, _ =>
            {
                processedCount++;
                if (count != 0)
                    progress.Value = Math.Min(0.99, ((double) processedCount) / count);
            });

            var description = new Property<string>("DeferredCacheProgressBarDescription",
                Strings.DeferredCacheProgressBar_Start_Processing_assets);
            var task = BackgroundProgressBuilder.Create()
                .WithTitle(Strings.DeferredCacheProgressBar_Start_Calculating_asset_index)
                .WithDescription(description)
                .WithProgress(progress)
                .Build();

            myCurrentFile.AdviseNotNull(startLifetime,
                v =>
                {
                    description.Value = string.Format(Strings.DeferredCacheProgressBar_Start_Processing_FileName,
                        v.DisplayName);
                });

            myLocks.Tasks.StartNew(startLifetime, Scheduling.MainDispatcher,
                () => { myBackgroundProgressManager.AddNewTask(lifetimeDef.Lifetime, task); });
        }

        public void SetCurrentProcessingFile(IPsiSourceFile sourceFile)
        {
            myCurrentFile.Value = sourceFile;
        }
    }
}