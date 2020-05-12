using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    public static class HackTaskRunnerFramework
    {
        public static void Install()
        {
            if (PlatformUtil.IsRunningUnderWindows)
                return;

            // COMPLETE HACK!
            // If the IDE has one version of JetBrains.ReSharper.TaskRunnerFramework, and the SDK has another, we'll get
            // an exception, because we have the IDE version loaded in memory, and the SDK expects to load the version
            // from the SDK, but Mono seems to preempt this and uses the one in memory. ReSharper's catalog compares
            // MVIDs, and yes, they're different.
            // We'll hack it to always copy the executing assembly, so ReSharper's catalog expects the same MVID as is
            // already running. There's a good chance that there are breaking API changes between the two versions, but
            // this shouldn't affect us as we don't use anything from the DLL.
            // There's probably a better fix but I have no idea what it is and suspect it can't be done from the plugin
            try
            {
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .SingleOrDefault(a => a.GetName().Name.Equals("JetBrains.ReSharper.TaskRunnerFramework"));
                if (loadedAssembly != null)
                {
                    var binDir = FileSystemPath.Parse(Assembly.GetExecutingAssembly().Location).Directory;
                    var outputAssembly = binDir.Combine("JetBrains.ReSharper.TaskRunnerFramework.dll");
                    DateTime? timestamp = null;
                    if (outputAssembly.ExistsFile) timestamp = outputAssembly.FileModificationTimeUtc;
                    File.Copy(loadedAssembly.Location, binDir.FullPath, true);
                    if (timestamp != null) outputAssembly.FileModificationTimeUtc = timestamp.Value;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}