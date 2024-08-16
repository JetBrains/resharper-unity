#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeCompletion;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Semantic;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.BlockShaders.Psi.Tree;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Cpp.Tree.Util;
using JetBrains.ReSharper.Psi.Cpp.Types;
using JetBrains.UI.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.CodeCompletion
{
    [Language(typeof(CppLanguage))]
    public class BlockShadersSemanticItemsProvider : ItemsProviderOfSpecificContext<CppCodeCompletionContext>
    {
        
        protected override bool IsAvailable(CppCodeCompletionContext context) => context.LanguageDialect is UnityBlockShadersDialect;

        protected override bool AddLookupItems(CppCodeCompletionContext context, IItemsCollector collector)
        {
            if (context.UnterminatedContext.TreeNode is not { Parent: BSAttributeParameterValue attributeParameterValue } ||
                !BSAttributeParameterValueNavigator.IsSemanticAttributeValue(attributeParameterValue) || 
                BSAttributeParameterValueNavigator.GetAttribute(attributeParameterValue) is not {} attribute ||
                context.LanguageDialect is not UnityBlockShadersDialect { Semantics: { Length: > 0 } semantics } ||
                BSAttributeNavigator.GetCorrespondingDeclaration(attribute) is not { } declaration)
                return false;

            var scope = HlslSemanticScope.Any;
            if (declaration.TryGetSingleDeclaratorNode() is Declarator declarator) {
                
                var qualType = declarator.GetResolveEntity().GetCppType();
                var cppType = qualType.InternalType switch
                {
                    CppFunctionType cppFunction => cppFunction.ReturnType,
                    _ => qualType
                };
            
                var isUnknownType = cppType.IsUnknownType();
                foreach (var hlslSemantic in semantics)
                {
                    if ((hlslSemantic.Scope & scope) != 0 && (isUnknownType || hlslSemantic.IsTypeSupported(cppType)))
                    {
                        AttributeSemanticsNames.TryGetValue(hlslSemantic.Name, out var attrName);
                        attrName ??= TryGetAttributeEnumerableSemanticName(hlslSemantic);
                        if (attrName == null)
                            continue;
                        AddLookupItem(context, collector, attrName);
                    }
                }
            }
            else
            {
                foreach (var attrSemantics in AttributeSemanticsNames.Values.
                             Concat(new []{"VertexAttributeColor", "VertexAttributeTextureCoordinate"}))
                {
                    AddLookupItem(context, collector, attrSemantics);
                }
            }
            return true;
        }

        private void AddLookupItem(CppCodeCompletionContext context, IItemsCollector collector, string attributeName)
        {
            var info = new AttributeSemanticTextualInfo(attributeName) { Ranges = context.CompletionRanges };
            var item = LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(static item => new TextPresentation<TextualInfo>(item.Info, item.Info.Icon, emphasize: false))
                .WithBehavior(static item => new TextualBehavior<TextualInfo>(item.Info))
                .WithMatcher(static item => new TextualMatcher<TextualInfo>(item.Info));
            collector.Add(item);
        }

        public class AttributeSemanticTextualInfo : TextualInfo
        {
            public IconId? Icon => null; 
            
            public AttributeSemanticTextualInfo(string name) : base(name, name)
            {
                Placement.Relevance = (ulong)CppCompletionRanks.Keyword;
            }
        }
        
        private static readonly Dictionary<string, string> AttributeSemanticsNames = new()
        {
            { "POSITION", "VertexAttributePosition"}, { "SV_POSITION", "Position"},
            { "NORMAL", "VertexAttributeNormal"}, { "TANGENT",  "VertexAttributeTangent"},
            { "COLOR", "Color"}, { "BLENDWEIGHT", "VertexAttributeBlendWeight"},
            { "BLENDINDICES", "VertexAttributeBlendIndices"}, { "PSIZE", "PointSize"},
            { "SV_ClipDistance", "ClipDistance"}, { "SV_CullDistance", "CullDistance"},
            { "SV_Coverage", "Coverage"}, { "SV_DepthGreaterEqual", "DepthGreaterEqual"}, 
            { "SV_DepthLessEqual", "DepthLessEqual"}, { "SV_Depth", "Depth"},
            { "SV_InstanceID", "InstanceID"}, { "SV_IsFrontFace", "IsFrontFace"},
            { "SV_SampleIndex", "SampleIndex"}, { "SV_StencilRef", "StencilReference"},
            { "SV_VertexID", "VertexID"}, { "SV_ShadingRate", "ShadingRate"},
            { "SV_ViewportArrayIndex", "ViewportArrayIndex"},
        };

        private static string? TryGetAttributeEnumerableSemanticName(HlslSemantic semantic)
        {
            var name = semantic.Name;
            if (name.LastOrDefault() is var last && !last.IsDigitFast())
                return null;

            if (name.StartsWith("COLOR"))
                return "VertexAttributeColor, " + last;
        
            if (name.StartsWith("TEXCOORD"))
                return "VertexAttributeTextureCoordinate, " + last;
            return null;
        }
    }
}