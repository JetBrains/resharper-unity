using System;
using System.Collections.Generic;
using System.Threading;
using Mono.Debugging.Backend;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.API;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values
{
    public class ErrorValue : IValue
    {
        private readonly string myMessage;

        public ErrorValue(string name, string message)
        {
            myMessage = message;
            SimpleName = name;
        }

        public IValueKeyPresentation GetKeyPresentation(IPresentationOptions options,
                                                        CancellationToken token = new())
        {
            return new ValueKeyPresentation(SimpleName, ValueOriginKind.Other, ValueFlags.None, DeclaredType);
        }

        public IValuePresentation GetValuePresentation(IPresentationOptions options,
                                                       CancellationToken token = new())
        {
            return SimplePresentation.Create(PresentationBuilder.New().Error(myMessage), PresentationKind.FailedObject, StatisticsKind.Error, 
                ValueFlags.NoChildren, DeclaredType);
        }

        public IEnumerable<IValueEntity> GetChildren(IPresentationOptions options, CancellationToken token = new())
        {
            yield break;
        }

        public string SimpleName { get; }
        public IValueRole GetPrimaryRole(IValueFetchOptions options) => throw new NotSupportedException();
        public IMetadataTypeLite? DeclaredType => null;
    }
}