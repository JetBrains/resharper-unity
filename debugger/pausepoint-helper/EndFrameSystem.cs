using System.Threading;
using UnityEngine;
using UnityEditor;

namespace JetBrains.Debugger.Worker.Plugins.Unity.PausePoint
{
    [InitializeOnLoad]
    public static class EndFrameSystem
    {
        static EndFrameSystem()
        {
            EditorApplication.update += Update;
        }

        private static volatile int _requiresPause;

        public static void MakePause()
        {
            Interlocked.Exchange(ref _requiresPause, 1);
        }

        private static void Update()
        {
            if (Interlocked.Exchange(ref _requiresPause, 0) > 0 && UnityEditor.EditorApplication.isPlaying)
                Debug.Break();
        }
    }
}