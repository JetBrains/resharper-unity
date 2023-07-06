#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve
{
    public class TexturePassReference<TOwner> : ReferenceWithOrigin<TOwner>, ITexturePassReference where TOwner : ITreeNode
    {
        private readonly ShaderReference<TOwner> myShaderReference;
        
        public TexturePassReference(ShaderReference<TOwner> shaderReference, TOwner owner, IReferenceOrigin<TOwner> origin) : base(owner, origin)
        {
            myShaderReference = shaderReference;
        }

        protected override ISymbolTable GetLookupSymbolTable()
        {
            var shader = myShaderReference.Resolve().DeclaredElement;
            if (shader == null)
                return EmptySymbolTable.INSTANCE;
            return new DeclaredElementsSymbolTable<IDeclaredElement>(myOwner.GetPsiServices(), GetTexturePasses(shader));

            static IEnumerable<IDeclaredElement> GetTexturePasses(IDeclaredElement shader)
            {
                foreach (var sourceFile in shader.GetSourceFiles())
                {
                    if (sourceFile.GetDominantPsiFile<ShaderLabLanguage>() is not {} file)
                        continue;
                    foreach (var node in file.Descendants<TexturePassDeclaration>())
                    {
                        if (node.DeclaredElement is {} element)
                            yield return element;
                    }
                }    
            }
        }
    }
}