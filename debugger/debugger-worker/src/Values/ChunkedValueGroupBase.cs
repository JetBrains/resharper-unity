using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values
{
    public abstract class ChunkedValueGroupBase<TRole> : ValueGroupBase
        where TRole : IValueRole
    {
        private readonly int myChunkSize;

        protected ChunkedValueGroupBase(string simpleName, int chunkSize = 100)
            : base(simpleName)
        {
            myChunkSize = chunkSize;
        }

        protected IEnumerable<IValueEntity> GetChunkedChildren(TRole collection, int startIndex, int length,
                                                               IValueFetchOptions options, CancellationToken token)
        {
            if (length > myChunkSize)
                return SplitIntoChunks(collection, startIndex, length, options, token);

            // Get an enumerable of child values. Calculating this is expensive, so do it on demand inside an iterator.
            // When a chunk group is created, it receives an enumerable of values. If we're not careful, each chunk's
            // children are eagerly evaluated and the whole point of chunking is lost.
            // The default array children renderer uses an array for this enumerable, but populates it with lightweight
            // array element value references that calculate the actual value on demand, via ICachedValueReference.
            // If we hit problems with this simple lazy enumerable, we could adopt a similar approach.
            return GetChildrenLazy(collection, startIndex, length, options, token);
        }

        private IEnumerable<IValueEntity> SplitIntoChunks(TRole collection, int chunkStartIndex, int length,
                                                          IValueFetchOptions options, CancellationToken token)
        {
            // Chunks of chunks
            var step = myChunkSize;
            var div = length / step;
            while (div > myChunkSize || div == myChunkSize && length % step > 0)
            {
                token.ThrowIfCancellationRequested();

                step *= myChunkSize;
                div = length / step;
            }

            for (var i = 0; i < length; i += step)
            {
                token.ThrowIfCancellationRequested();

                var startIndex = chunkStartIndex + i;
                var endIndex = Math.Min(chunkStartIndex + i + step, chunkStartIndex + length) - 1;
                var chunkLength = endIndex - startIndex + 1;
                var name = $"[{i}..{endIndex}]";
                yield return new SimpleEntityGroup(name,
                    GetChunkedChildren(collection, startIndex, chunkLength, options, token));
            }
        }

        private IEnumerable<IValueEntity> GetChildrenLazy(TRole collection, int startIndex, int length,
                                                          IValueFetchOptions options, CancellationToken token)
        {
            for (var i = 0; i < length; i++)
            {
                token.ThrowIfCancellationRequested();

                yield return GetElementValueAt(collection, startIndex + i, options);
            }
        }

        [NotNull]
        protected abstract IValue GetElementValueAt(TRole collection, int index, IValueFetchOptions options);
    }
}