namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public class UnityProblemAnalyzerContextSetting : IUnityProblemAnalyzerContextClassification
    {
        public UnityProblemAnalyzerContextElement Context { get; }

        public bool IsAvailable { get; }

        public UnityProblemAnalyzerContextSetting(bool isAvailable, UnityProblemAnalyzerContextElement context)
        {
            IsAvailable = isAvailable;
            Context = context;
        }
    }
}