using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity.BackendUnity;

namespace JetBrains.Rider.Unity.Editor
{
    /// <summary>
    /// Instead of writing project files to disk, Rider package would try to write them to a ScriptableObject,
    /// ScriptableObject would get synced to `files` and `Sync` would get called.
    /// Rider would sync data to disk
    /// </summary>
    public static class ProjectFilesSync
    {
        [UsedImplicitly] internal static Dictionary<string, string> files = new Dictionary<string, string>();

        [UsedImplicitly]
        public static bool Sync()
        {
            var models = PluginEntryPoint.UnityModels.Where(a => a.Lifetime.IsAlive).ToArray();
            if (models.Any())
            {
                var modelLifetime = models.First();
                var model = modelLifetime.Model;
                Sync(model, modelLifetime.Lifetime);
                return true;
            }

            return false;
        }

        public static void Sync(BackendUnityModel model, Lifetime connectionLifetime)
        {
            MainThreadDispatcher.Instance.Queue(() =>
            {
                var list = files.Select(a => new FileChangeArgs(a.Key, a.Value)).ToList();

                var task = model.WriteFileWithRider.Start(connectionLifetime, list);
                task.Result.Advise(connectionLifetime, result =>
                {
                    if (result.Result)
                        files = new Dictionary<string, string>();
                });
            });
        }
    }
}