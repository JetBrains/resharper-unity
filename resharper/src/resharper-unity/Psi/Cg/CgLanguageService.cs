using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing;
using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg
{
    [Language(typeof(CgLanguage))]
    public class CgLanguageService : LanguageService
    {
        public CgLanguageService(PsiLanguageType psiLanguageType, IConstantValueService constantValueService)
            : base(psiLanguageType, constantValueService)
        {
        }

        public override ILexerFactory GetPrimaryLexerFactory()
        {
            return new CgLexerFactory();
        }

        public override ILexer CreateFilteringLexer(ILexer lexer)
        {
            return new CgFilteringLexer(lexer);
        }

        public override IParser CreateParser(ILexer lexer, IPsiModule module, IPsiSourceFile sourceFile)
        {
            return new DummyParser();
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
        
        private class DummyParser : IParser
        {
            public IFile ParseFile()
            {
                return new DummyFile();
            }

            private class DummyFile : FileElementBase
            {
                public override NodeType NodeType => CgTokenNodeTypes.BAD_CHARACTER;
                public override PsiLanguageType Language => (PsiLanguageType) CgLanguage.Instance ?? UnknownLanguage.Instance;
            }
        }
    }
}