using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [GeneratorElementProvider(GeneratorUnityKinds.UnityEventFunctions, typeof(CSharpLanguage))]
    public class TestGenerateUnityEventFunctionsProvider : IGeneratorElementProvider
    {
        private readonly JetHashSet<string> myInputElements = new JetHashSet<string>();

        public void SelectElement(string name)
        {
            myInputElements.Add(name);
        }

        public void Clear()
        {
            myInputElements.Clear();
        }

        public void Populate(IGeneratorContext context)
        {
            context.InputElements.AddRange(context.ProvidedElements.OfType<GeneratorDeclaredElement>()
                .Where(e => myInputElements.Contains(e.DeclaredElement.ShortName)));
        }

        // Must be greater than GenerateUnityEventFunctionsProvider.Priority
        public double Priority => 1000;
    }
}