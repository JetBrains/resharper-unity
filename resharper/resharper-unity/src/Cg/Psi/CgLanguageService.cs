using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi
{
    [Language(typeof(CgLanguage))]
    public class CgLanguageService : LanguageService
    {
        private readonly CommonIdentifierIntern myIntern;

        public CgLanguageService(PsiLanguageType psiLanguageType, IConstantValueService constantValueService, CommonIdentifierIntern intern)
            : base(psiLanguageType, constantValueService)
        {
            myIntern = intern;
        }

        public override ILexerFactory GetPrimaryLexerFactory()
        {
            return new CgLexerFactory();
        }

        public override ILexer CreateFilteringLexer(ILexer lexer)
        {
            return new CgFilteringLexer(lexer, null);
        }

        public override IParser CreateParser(ILexer lexer, IPsiModule module, IPsiSourceFile sourceFile)
        {
            return new CgParser(lexer as ILexer<int> ?? lexer.ToCachingLexer(), myIntern);
        }

        public override IEnumerable<ITypeDeclaration> FindTypeDeclarations(IFile file)
        {
            return EmptyList<ITypeDeclaration>.Enumerable; // TODO: probably want to fix that
        }

        public override ILanguageCacheProvider CacheProvider => null;

        public override bool IsCaseSensitive => true;

        public override bool SupportTypeMemberCache => false;

        public override ITypePresenter TypePresenter => DefaultTypePresenter.Instance;

        private class CgLexerFactory : ILexerFactory
        {
            public ILexer CreateLexer(IBuffer buffer)
            {
                return new CgLexerGenerated(buffer);
            }
        }
    }
}