#nullable enable

using JetBrains.ReSharper.Feature.Services.CodeStructure;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeStructure
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabCodeStructureProvider : HierarchicalDeclarationPsiFileCodeStructureProviderBase
    {
        protected override CodeStructureElement? CreateDeclarationElement(CodeStructureElement parent, IHierarchicalDeclaration declaration, CodeStructureOptions options)
        {
            var element = base.CreateDeclarationElement(parent, declaration, options);
            if (declaration is ICodeBlock codeBlock && element != null)
                BuildHlslCodeStructure(element, codeBlock, options);
            return element;
        }

        private void BuildHlslCodeStructure(CodeStructureElement element, ICodeBlock codeBlock, CodeStructureOptions options)
        {
            if (codeBlock.GetSourceFile() is { } sourceFile 
                && sourceFile.GetPsiFile<CppLanguage>(codeBlock.Content.GetDocumentRange()) is {} cppFile 
                && LanguageManager.Instance.TryGetCachedService<IPsiFileCodeStructureProvider>(cppFile.Language) is {} cgCodeStructureProvider)
            {
                var cppCodeStructure = cgCodeStructureProvider.Build(cppFile, options);
                foreach (var child in cppCodeStructure.Children) 
                    element.AppendChild(child);
            }
        }
    }
}