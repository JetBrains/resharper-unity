#if RIDER

using System;
using System.Linq.Expressions;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.Unity.Host.Features.Foldings.ShaderLab
{
    // TODO: We really need a proper reference to JetBrains.ReSharper.Host.exe
    // Then, we can just call new :)
    internal class CodeFoldingHighlightingCreator
    {
        private static readonly object ourLock = new object();
        private static Func<string, string, DocumentRange, int, IHighlighting> ourConstructor;

        public static IHighlighting Create(string attributeId, string placeholderText, DocumentRange range, int priority)
        {
            if (ourConstructor == null)
            {
                lock (ourLock)
                {
                    if (ourConstructor == null)
                        ourConstructor = Something();
                }
            }

            return ourConstructor?.Invoke(attributeId, placeholderText, range, priority);
        }

        private static Func<string, string, DocumentRange, int, IHighlighting> Something()
        {
            var type = Type.GetType("JetBrains.ReSharper.Host.Features.Foldings.CodeFoldingHighlighting, JetBrains.ReSharper.Host");
            if (type != null)
            {
                var constructorInfo = type.GetConstructor(new[] { typeof(string), typeof(string), typeof(DocumentRange), typeof(int)});
                if (constructorInfo != null)
                {
                    var attributeIdParameter = Expression.Parameter(typeof(string), "attributeId");
                    var placeholderTextParameter = Expression.Parameter(typeof(string), "placeholderText");
                    var rangeParameter = Expression.Parameter(typeof(DocumentRange), "range");
                    var priorityParameter = Expression.Parameter(typeof(int), "priority");
                    var ctor = Expression.New(constructorInfo, attributeIdParameter, placeholderTextParameter,
                        rangeParameter, priorityParameter);
                    var lambda = Expression.Lambda<Func<string, string, DocumentRange, int, IHighlighting>>(ctor,
                        attributeIdParameter, placeholderTextParameter, rangeParameter, priorityParameter);
                    return lambda.Compile();
                }
            }

            return null;
        }
    }
}

#endif