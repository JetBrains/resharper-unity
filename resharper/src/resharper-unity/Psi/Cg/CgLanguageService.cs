using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
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
            throw new NotImplementedException();
        }

        public override IEnumerable<ITypeDeclaration> FindTypeDeclarations(IFile file)
        {
            return EmptyList<ITypeDeclaration>.Enumerable; // TODO: probably want to fix that
        }

        public override ILanguageCacheProvider CacheProvider { get; }
        
        public override bool IsCaseSensitive { get; }
        
        public override bool SupportTypeMemberCache { get; }
        
        public override ITypePresenter TypePresenter { get; }

        private class CgLexerFactory : ILexerFactory
        {
            public ILexer CreateLexer(IBuffer buffer)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}