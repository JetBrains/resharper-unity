using System.Collections.Generic;
using System.Threading;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    public abstract class ValueGroupBase : IValueGroup
    {
        protected ValueGroupBase(string simpleName)
        {
            SimpleName = simpleName;
        }

        public string SimpleName { get; }
        public virtual bool IsTop => true;

        public virtual IValueKeyPresentation GetKeyPresentation(IPresentationOptions options,
                                                                CancellationToken token = new CancellationToken())
        {
            return new ValueKeyPresentation(SimpleName, ValueOriginKind.Group, ValueFlags.None);
        }

        public virtual IValuePresentation GetValuePresentation(IPresentationOptions options,
                                                               CancellationToken token = new CancellationToken())
        {
            return SimplePresentation.EmptyPresentation;
        }

        public abstract IEnumerable<IValueEntity> GetChildren(IPresentationOptions options,
                                                              CancellationToken token = new CancellationToken());
    }
}