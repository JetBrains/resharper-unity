using System;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    public readonly struct AnimatorScript
    {
        public AnimatorScript(Guid guid, long anchor)
        {
            Guid = guid;
            Anchor = anchor;
        }

        public Guid Guid { get; }
        public long Anchor { get; }
    }
}