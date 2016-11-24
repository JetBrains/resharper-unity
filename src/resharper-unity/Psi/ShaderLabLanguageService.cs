using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabLanguageService : LanguageService
    {
        public ShaderLabLanguageService(ShaderLabLanguage psiLanguageType, IConstantValueService constantValueService)
            : base(psiLanguageType, constantValueService)
        {
        }

        public override ILexerFactory GetPrimaryLexerFactory()
        {
            return new ShaderLabLexerFactory();
        }

        public override ILexer CreateFilteringLexer(ILexer lexer)
        {
            return new ShaderLabFilteringLexer(lexer);
        }

        public override IParser CreateParser(ILexer lexer, IPsiModule module, IPsiSourceFile sourceFile)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<ITypeDeclaration> FindTypeDeclarations(IFile file)
        {
            return EmptyList<ITypeDeclaration>.Enumerable;
        }

        public override ILanguageCacheProvider CacheProvider => null;
        public override bool IsCaseSensitive => true;
        public override bool SupportTypeMemberCache => false;
        public override ITypePresenter TypePresenter => DefaultTypePresenter.Instance;

        private class ShaderLabLexerFactory : ILexerFactory
        {
            public ILexer CreateLexer(IBuffer buffer)
            {
                return (ILexer) new ShaderLabLexer(buffer);
            }
        }
        
        // TEMPORARY
        private class ShaderLabLexer : ILexer
        {
            public ShaderLabLexer(IBuffer buffer)
            {
                Buffer = buffer;
            }

            public void Start()
            {
            }

            public void Advance()
            {
            }

            public object CurrentPosition { get; set; }
            public TokenNodeType TokenType => null;
            public int TokenStart => 0;
            public int TokenEnd => 0;
            public IBuffer Buffer { get; }
        }
    }
}