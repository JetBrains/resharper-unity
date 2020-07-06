namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render
{
    public static class UnityRendererUtil
    {
        // This is higher than ObjectChildrenRenderer so we will get a chance, but lower than
        // DebuggerTypeProxyChildrenRenderer, so if Unity ever add a DebuggerTypeProxy, it will take precedence
        public const int ChildrenRendererPriority = 50;

        // Higher than default, but lower than anything else. If Unity add a DebuggerDisplay attribute, it will take
        // precedence
        public const int ValueRendererPriority = 5;
    }
}