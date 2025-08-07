#nullable enable
using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabLanguageService(ShaderLabLanguage psiLanguageType, ILazy<IConstantValueService> constantValueService, ILazy<CommonIdentifierIntern> commonIdentifierIntern, ILazy<ShaderLabCodeFormatter> codeFormatter)
        : LanguageService(psiLanguageType, constantValueService, codeFormatter)
    {
        private IDeclaredElementPresenter? myPresenter;

        public override ILexerFactory GetPrimaryLexerFactory() => new ShaderLabLexerFactory();

        public override ILexer CreateFilteringLexer(ILexer lexer)
        {
            // TODO: Is it ok that this is without preprocessor state?
            // This is what the C# language service does
            return new ShaderLabFilteringLexer(lexer, null);
        }

        public override IParser CreateParser(ILexer lexer, IPsiModule? module, IPsiSourceFile? sourceFile) => new ShaderLabParser(lexer as ILexer<int> ?? lexer.ToCachingLexer(), commonIdentifierIntern.Value);

        public override IEnumerable<ITypeDeclaration> FindTypeDeclarations(IFile file) => EmptyList<ITypeDeclaration>.Enumerable;

        public override ILanguageCacheProvider? CacheProvider => null;
        public override bool IsCaseSensitive => true;
        public override bool SupportTypeMemberCache => false;
        public override ITypePresenter TypePresenter => DefaultTypePresenter.Instance;
        public override IDeclaredElementPresenter DeclaredElementPresenter => myPresenter ??= ShaderLabDeclaredElementPresenter.Instance;

        public override bool IsValidName(DeclaredElementType elementType, string name)
        {
            if (elementType == ShaderLabDeclaredElementType.Shader)
                return IsValidShaderName(name);
            return base.IsValidName(elementType, name);
        }

        private bool IsValidShaderName(string name) => !string.IsNullOrEmpty(name);

        private class ShaderLabLexerFactory : ILexerFactory
        {
            public ILexer CreateLexer(IBuffer buffer)
            {
                return new ShaderLabLexerGenerated(buffer);
            }
        }
    }
}