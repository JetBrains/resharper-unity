﻿using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.ReSharper.Plugins.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Json.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Json.Psi
{
    [Language(typeof(JsonNewLanguage))]
    public class JsonNewLanguageService(PsiLanguageType psiLanguageType, ILazy<IConstantValueService> constantValueService)
        : LanguageService(psiLanguageType, constantValueService)
    {
        public override ILexerFactory GetPrimaryLexerFactory()
        {
            return new JsonNewLexerFactory();
        }

        public override ILexer CreateFilteringLexer(ILexer lexer)
        {
            return new JsonNewFilteringLexer(lexer);
        }

        public override IParser CreateParser(ILexer lexer, IPsiModule module, IPsiSourceFile sourceFile)
        {
            return new JsonNewParser(lexer as ILexer<int> ?? lexer.ToCachingLexer());
        }

        public override IEnumerable<ITypeDeclaration> FindTypeDeclarations(IFile file)
        {
            return EmptyList<ITypeDeclaration>.Enumerable;
        }

        public override ILanguageCacheProvider CacheProvider => null;

        public override bool IsCaseSensitive => true;

        public override bool SupportTypeMemberCache => false;

        public override ITypePresenter TypePresenter => DefaultTypePresenter.Instance;

        public override bool IsValidName(DeclaredElementType elementType, string name)
        {
            if (elementType is JsonNewDeclaredElementType jsonNewDeclaredElementType)
                return jsonNewDeclaredElementType.IsValidName(name);

            return base.IsValidName(elementType, name);
        }

        private class JsonNewLexerFactory : ILexerFactory
        {
            public ILexer CreateLexer(IBuffer buffer)
            {
                return new JsonNewLexerGenerated(buffer);
            }
        }
    }
}