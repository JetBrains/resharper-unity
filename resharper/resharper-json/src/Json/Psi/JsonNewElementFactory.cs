using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Json.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Text;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Json.Psi
{
    public class JsonNewElementFactory
    {
        private readonly IPsiModule myModule;
        private readonly LanguageService myLanguageService;

        public static JsonNewElementFactory GetInstance(IPsiModule psiModule) => new(psiModule,
            JsonNewLanguage.Instance.NotNull());

        private JsonNewElementFactory(IPsiModule module, PsiLanguageType language)
        {
            myModule = module;
            myLanguageService = language.LanguageService().NotNull();
        }

        private JsonNewParser CreateParser(string text)
        {
            var lexerFactory = myLanguageService.GetPrimaryLexerFactory();
            var lexer = lexerFactory.CreateLexer(new StringBuffer(text));

            return (JsonNewParser)myLanguageService.CreateParser(lexer, myModule, null);
        }

        public IJsonNewLiteralExpression CreateStringLiteral(string literal)
        {
            var parser = CreateParser($"\"{literal}\"");
            var node = parser.ParseLiteral();
            if (node == null)
                throw new ElementFactoryException($"Cannot create expression \"{literal}\"");
            SandBox.CreateSandBoxFor(node, myModule, myLanguageService.LanguageType);
            return node;
        }

        public IJsonNewValue CreateValue(string value)
        {
            var parser = CreateParser(value);
            var node = parser.ParseValue();
            if (node == null)
                throw new ElementFactoryException($"Cannot create expression '{value}'");
            SandBox.CreateSandBoxFor(node, myModule, myLanguageService.LanguageType);
            return node;
        }
    }
}