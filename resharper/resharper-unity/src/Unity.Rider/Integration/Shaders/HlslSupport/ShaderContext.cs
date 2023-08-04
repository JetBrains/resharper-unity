#nullable enable
using System;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport
{
    public class ShaderContext : IDisposable
    {
        private readonly LifetimeDefinition myLifetimeDefinition = new();
        private readonly SequentialLifetimes myQueryLifetimes;
        private int myRefsCount = 1;
        
        public Lifetime Lifetime => myLifetimeDefinition.Lifetime;
        public readonly RdDocumentId DocumentId;
        public readonly IPsiSourceFile SourceFile;
        public IPsiSourceFile? RootFile;
        public IRangeMarker? RootRangeMarker;

        public ShaderContext(RdDocumentId documentId, IPsiSourceFile sourceFile)
        {
            DocumentId = documentId;
            SourceFile = sourceFile;
            myQueryLifetimes = new(Lifetime);
        }

        public Lifetime NextQueryLifetime() => myQueryLifetimes.Next();

        public void IncrementRefCount()
        {
            Assertion.Assert(myRefsCount > 0, "Can't add ref to already disposed ShaderContext");
            ++myRefsCount;
        }

        public void DecrementRefCount()
        {
            Assertion.Assert(myRefsCount > 0, "Can't decrement ref to already disposed ShaderContext");
            if (--myRefsCount == 0)
                Dispose();
        }

        public void Dispose() => myLifetimeDefinition.Dispose();
    }
}