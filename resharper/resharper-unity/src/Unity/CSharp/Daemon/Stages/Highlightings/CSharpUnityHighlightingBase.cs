namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    // Add a marker interface to all of our highlights. If we specify a baseClass in ErrorsGen, we have to provide an
    // actual class with an abstract IsValid method, because ErrorsGen will declare IsValid as an override.
    public abstract class CSharpUnityHighlightingBase : IUnityHighlighting
    {
        public abstract bool IsValid();
    }
}