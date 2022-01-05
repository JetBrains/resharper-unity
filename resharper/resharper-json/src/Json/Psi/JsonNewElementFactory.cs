using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Json.Psi
{
    public class JsonNewElementFactory
    {
        [NotNull] private readonly IPsiModule myModule;
        [NotNull] private readonly ISolution mySolution;
        [NotNull] private readonly PsiLanguageType myLanguage;
        [NotNull]private readonly LanguageService myLanguageService;

        public static JsonNewElementFactory GetInstance(IPsiModule psiModule)
        {
            return new JsonNewElementFactory(psiModule, psiModule.GetSolution(), JsonNewLanguage.Instance);
        }
        
        private JsonNewElementFactory([NotNull] IPsiModule module, [NotNull] ISolution solution, [NotNull] PsiLanguageType language)
        {
            myModule = module;
            mySolution = solution;
            myLanguage = language;
            myLanguageService = language.LanguageService();
        }

        private JsonNewParser CreateParser(string text)
        {
            var lexerFactory = myLanguageService.GetPrimaryLexerFactory();
            var lexer = lexerFactory.CreateLexer(new StringBuffer(text));

            return myLanguageService.CreateParser(lexer, myModule, null) as JsonNewParser;
        }
        
        public IJsonNewLiteralExpression CreateStringLiteral(string literal)
        {
            var parser = CreateParser($"\"{literal}\"");
            var node = parser.ParseLiteral();
            if (node == null)
                throw new ElementFactoryException(string.Format("Cannot create expression '{0}'", literal));
            SandBox.CreateSandBoxFor(node, myModule, myLanguageService.LanguageType);
            return node;
        }
    }
}