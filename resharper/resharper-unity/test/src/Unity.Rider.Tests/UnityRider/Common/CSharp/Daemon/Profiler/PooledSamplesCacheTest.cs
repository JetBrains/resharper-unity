using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Common.CSharp.Daemon.Profiler
{
    [TestFixture]
    public class PooledSamplesCacheTest
    {
        // RIDER-139836: GetFrontendModelSnapshot() must hand the protocol an independent copy of the samples.
        // The cache stores them in a pooled list that is cleared and recycled on Dispose, so a snapshot built
        // over the live pooled list is silently emptied/overwritten once the cache is disposed and the pool
        // hands the storage to the next snapshot - which the protocol then serializes, crashing the backend.
        [Test]
        public void GetFrontendModelSnapshot_SurvivesCacheDisposalAndRecycling()
        {
            var cache = SamplesCacheUtils.ConstructCache(CreateSnapshot(
                "Assembly-CSharp.dll!Game::Player.Update()",
                "Assembly-CSharp.dll!Game::Player.Move()"));
            var frontendSnapshot = cache.GetFrontendModelSnapshot();

            var expectedCount = frontendSnapshot.Samples.Count;
            var expectedFirstName = frontendSnapshot.Samples[0].QualifiedName;
            Assert.That(expectedCount, Is.EqualTo(2));

            // Dispose returns the pooled list to the pool; building another cache hands that recycled storage
            // to fresh, differently-populated samples. The earlier snapshot must be unaffected. Dispose exactly
            // once (no using on the first cache) so the instance is not returned to the pool twice.
            cache.Dispose();
            using (SamplesCacheUtils.ConstructCache(CreateSnapshot("Assembly-CSharp.dll!Game::Enemy.Patrol()")))
            {
                Assert.That(frontendSnapshot.Samples.Count, Is.EqualTo(expectedCount),
                    "Published snapshot must survive cache disposal/recycling");
                Assert.That(frontendSnapshot.Samples[0].QualifiedName, Is.EqualTo(expectedFirstName),
                    "Published snapshot data must not be mutated by cache recycling");
            }
        }

        private static UnityProfilerSnapshot CreateSnapshot(params string[] sampleNames)
        {
            var markers = new List<MarkerToNamePair>();
            var samples = new List<SampleInfo>();
            for (var i = 0; i < sampleNames.Length; i++)
            {
                var markerId = i + 1; // positive id => not a profiler marker
                markers.Add(new MarkerToNamePair(markerId, sampleNames[i]));
                samples.Add(new SampleInfo(duration: 1.0, markerId: markerId, memoryAllocation: 0L, childrenCount: 0));
            }

            return new UnityProfilerSnapshot(frameIndex: 0, startTimeMs: 0.0, frameTimeMs: 16.0f,
                thread: new ProfilerThread(0, "Main Thread"), markerIdToName: markers, samples: samples);
        }
    }
}
