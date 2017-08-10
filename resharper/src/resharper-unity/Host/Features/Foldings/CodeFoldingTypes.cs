#if RIDER

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;

// TODO: Delete these once we have a Rider SDK and can use the real interfaces

// ReSharper disable once CheckNamespace
namespace JetBrains.ReSharper.Host.Features.Foldings
{
    internal interface ICodeFoldingProcessorFactory
    {
        ICodeFoldingProcessor CreateProcessor();
    }
}

#endif