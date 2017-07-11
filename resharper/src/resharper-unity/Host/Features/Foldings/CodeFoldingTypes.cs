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

    public interface ICodeFoldingProcessor : IRecursiveElementProcessor<IHighlightingConsumer>
    {
    }

    public static class CodeFoldingAttributes
    {
        public const string DEFAULT_FOLDING_ATTRIBUTE = "ReSharper Default Folding";
    }
}

#endif