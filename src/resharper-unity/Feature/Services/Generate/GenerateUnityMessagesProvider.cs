using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Generate
{
    [GeneratorElementProvider(GeneratorUnityKinds.UnityMessages, typeof(CSharpLanguage))]
    public class GenerateUnityMessagesProvider : GeneratorProviderBase<CSharpGeneratorContext>
    {
        public override void Populate(CSharpGeneratorContext context)
        {
            var typeElement = context.ClassDeclaration.DeclaredElement as IClass;
            if (typeElement == null)
                return;

            var hosts = typeElement.GetMessageHosts().ToArray();
            var events = hosts.SelectMany(h => h.Messages)
                .Where(m => !typeElement.Methods.Any(m.Match)).ToArray();

            var classDeclaration = context.ClassDeclaration;
            var factory = CSharpElementFactory.GetInstance(classDeclaration);
            var methods = events
                .Select(e => e.CreateDeclaration(factory, classDeclaration))
                .Select(d => d.DeclaredElement)
                .Where(m => m != null);
            IEnumerable<IGeneratorElement> elements =
                methods.Select(m => new GeneratorDeclaredElement<IMethod>(m));
            context.ProvidedElements.AddRange(elements);
        }

        public override double Priority => 100;
    }
}