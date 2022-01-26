using System;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features.BackgroundTasks;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;
using JetBrains.Rider.Backend.Features.BackgroundTasks;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Caches
{
    [SolutionComponent]
    public class RiderDeferredCacheProgressBar : DeferredCacheProgressBar
    {
        private readonly Lifetime myLifetime;
        private readonly DeferredHelperCache myCache;
        private readonly IShellLocks myLocks;
        [CanBeNull] private readonly RiderBackgroundTaskHost myRiderBackgroundTaskHost;

        public RiderDeferredCacheProgressBar(Lifetime lifetime, DeferredHelperCache cache, IShellLocks locks, [CanBeNull] RiderBackgroundTaskHost riderBackgroundTaskHost = null)
            : base(lifetime, cache)
        {
            myLifetime = lifetime;
            myCache = cache;
            myLocks = locks;
            myRiderBackgroundTaskHost = riderBackgroundTaskHost;
        }

        public override void Start(Lifetime startLifetime)
        {
            if (myRiderBackgroundTaskHost != null)
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

                var description = new Property<string>(startLifetime, "DeferredCacheProgressBarDescription", "Processing assets");
                var task = RiderBackgroundTaskBuilder.Create()
                    .WithTitle("Calculating asset index")
                    .WithDescription(description)
                    .WithProgress(progress)
                    .Build();

                CurrentFile.AdviseNotNull(startLifetime, v => { description.Value = $"Processing {v.DisplayName}"; });

                myLocks.Tasks.StartNew(startLifetime, Scheduling.MainDispatcher,
                    () => { myRiderBackgroundTaskHost.AddNewTask(lifetimeDef.Lifetime, task); });
            }
        }
    }
}