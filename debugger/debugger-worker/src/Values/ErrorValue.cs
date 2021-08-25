using System.Collections.Generic;
using System.Threading;
using MetadataLite.API;
using Mono.Debugging.Backend;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values
{
    public class ErrorValue : IValue
    {
        private readonly string myMessage;
        private readonly ValueResultKind myResultKind;

        public ErrorValue(string name, string message, ValueResultKind resultKind = ValueResultKind.Error)
        {
            myMessage = message;
            myResultKind = resultKind;
            SimpleName = name;
        }

        public IValueKeyPresentation GetKeyPresentation(IPresentationOptions options,
                                                        CancellationToken token = new CancellationToken())
        {
            return new ValueKeyPresentation(SimpleName, ValueOriginKind.Other, ValueFlags.None, DeclaredType);
        }

        public IValuePresentation GetValuePresentation(IPresentationOptions options,
                                                       CancellationToken token = new CancellationToken())
        {
            return SimplePresentation.Create(PresentationBuilder.New().Error(myMessage), myResultKind,
                ValueFlags.NoChildren, DeclaredType);
        }

        public IEnumerable<IValueEntity> GetChildren(IPresentationOptions options, CancellationToken token = new CancellationToken())
        {
            yield break;
        }

        public string SimpleName { get; }
        public IValueRole GetPrimaryRole(IValueFetchOptions options) => null;
        public IMetadataTypeLite DeclaredType => null;
    }
}