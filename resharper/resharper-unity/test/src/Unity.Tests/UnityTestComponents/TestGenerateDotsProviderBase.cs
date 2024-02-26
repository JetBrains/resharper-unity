using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;

public abstract class TestGenerateDotsProviderBase : IGeneratorElementProvider
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

    public abstract double Priority { get; }

    public void Populate(IGeneratorContext context)
    {
        if(myInputElements.Count == 0)
            return;
            
        context.InputElements.AddRange(context.ProvidedElements.OfType<GeneratorDeclaredElement>()
            .Where(e => myInputElements.Contains(e.DeclaredElement.ShortName)));
    }
}