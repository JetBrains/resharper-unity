using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [GeneratorElementProvider(GeneratorUnityKinds.UnityGenerateRefAccessors, typeof(CSharpLanguage))]
    public class TestGenerateRefAccessorsProvider : IGeneratorElementProvider
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
            if(myInputElements.Count == 0)
                return;
            
            context.InputElements.AddRange(context.ProvidedElements.OfType<GeneratorDeclaredElement>()
                .Where(e => myInputElements.Contains(e.DeclaredElement.ShortName)));
        }
        // Must be greater than GenerateRefAccessorsProvider.Priority
        public double Priority => 1000;
    }
}