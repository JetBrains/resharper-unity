using System;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost.Progress;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Caches
{
    [SolutionComponent]
    public class RiderDeferredCacheProgressBar : DeferredCacheProgressBar
    {
        private readonly Lifetime myLifetime;
        private readonly DeferredHelperCache myCache;
        private readonly IShellLocks myLocks;
        [CanBeNull] private readonly BackgroundProgressManager myBackgroundProgressManager;

        public RiderDeferredCacheProgressBar(Lifetime lifetime, DeferredHelperCache cache, IShellLocks locks, [CanBeNull] BackgroundProgressManager backgroundProgressManager = null)
            : base(lifetime, cache)
        {
            myLifetime = lifetime;
            myCache = cache;
            myLocks = locks;
            myBackgroundProgressManager = backgroundProgressManager;
        }

        public override void Start(Lifetime startLifetime)
        {
            if (myBackgroundProgressManager != null)
            {
                // avoid problems with background task host after terminating lifetime on daemon thread
                var lifetimeDef = myLifetime.CreateNested();
                startLifetime.OnTermination(() =>
                {
                    myLocks.Tasks.StartNew(myLifetime, Scheduling.MainDispatcher, () => lifetimeDef.Terminate());
                });

                var count = myCache.FilesToProcess.Count;
                var processedCount = 0;
                var progress = new Property<double>(startLifetime, "DeferredCacheProgressBarProgress", 0);

                myCache.AfterRemoveFromProcess.Advise(startLifetime, _ =>
                {
                    processedCount++;
                    if (count != 0)
                        progress.Value = Math.Min(0.99, ((double) processedCount) / count);
                });

                var description = new Property<string>(startLifetime, "DeferredCacheProgressBarDescription", Strings.RiderDeferredCacheProgressBar_Start_Processing_assets);
                var task = BackgroundProgressBuilder.Create()
                    .WithTitle(Strings.RiderDeferredCacheProgressBar_Start_Calculating_asset_index)
                    .WithDescription(description)
                    .WithProgress(progress)
                    .Build();

                CurrentFile.AdviseNotNull(startLifetime, v => { description.Value = string.Format(Strings.RiderDeferredCacheProgressBar_Start_Processing__0_, v.DisplayName); });

                myLocks.Tasks.StartNew(startLifetime, Scheduling.MainDispatcher,
                    () => { myBackgroundProgressManager.AddNewTask(lifetimeDef.Lifetime, task); });
            }
        }
    }
}