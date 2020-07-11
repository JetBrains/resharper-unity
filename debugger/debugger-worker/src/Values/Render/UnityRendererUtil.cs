namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render
{
    public static class UnityRendererUtil
    {
        // This is higher than ObjectChildrenRenderer so we will get a chance, but lower than
        // DebuggerTypeProxyChildrenRenderer, so if Unity ever add a DebuggerTypeProxy, it will take precedence
        public const int ChildrenRendererPriority = 50;

        // Higher than ValuePresenterBase, but lower than DebuggerDisplayObjectPresenter, so any DebuggerDisplay
        // attributes take precedence. Note that it's also lower than Decimal, Enum and Nullable presenters
        public const int ValuePresenterPriority = 5;
    }
}