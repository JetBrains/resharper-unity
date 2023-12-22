#nullable enable
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
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Cpp.Types;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.CodeCompletion
{
    [Language(typeof(CppLanguage))]
    public class HlslSemanticItemsProvider : ItemsProviderOfSpecificContext<CppCodeCompletionContext>
    {
        private readonly HlslSemanticScopeDetector myDetector;

        public HlslSemanticItemsProvider(HlslSemanticScopeDetector detector)
        {
            myDetector = detector;
        }
        
        protected override bool IsAvailable(CppCodeCompletionContext context) => context.LanguageDialect is UnityHlslDialectBase;

        protected override bool AddLookupItems(CppCodeCompletionContext context, IItemsCollector collector)
        {
            if (context.UnterminatedContext.TreeNode is not { Parent: HlslTrailingSemantic semanticNode } 
                || context.LanguageDialect is not UnityHlslDialectBase { Semantics: { Length: > 0 } semantics })
                return false;

            var scope = myDetector.DetectScope(semanticNode);
            if (scope == HlslSemanticScope.Unknown)
                scope = HlslSemanticScope.Any;
            var declarator = semanticNode.GetContainingNode<Declarator>();
            var qualType = declarator?.GetResolveEntity().GetCppType() ?? new(CppUnknownType.LookupFailure);
            var cppType = qualType.InternalType switch
            {
                CppFunctionType cppFunction => cppFunction.ReturnType,
                _ => qualType
            };
            
            var isUnknownType = cppType.IsUnknownType();
            foreach (var hlslSemantic in semantics)
            {
                if ((hlslSemantic.Scope & scope) != 0 && (isUnknownType || hlslSemantic.IsTypeSupported(cppType)))
                    AddLookupItem(context, collector, hlslSemantic);
            }
            return true;
        }

        private void AddLookupItem(CppCodeCompletionContext context, IItemsCollector collector, HlslSemantic semantic)
        {
            var info = new SemanticTextualInfo(semantic) { Ranges = context.CompletionRanges };
            var item = LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(static item => new TextPresentation<TextualInfo>(item.Info, item.Info.Icon, emphasize: false))
                .WithBehavior(static item => new TextualBehavior<TextualInfo>(item.Info))
                .WithMatcher(static item => new TextualMatcher<TextualInfo>(item.Info));
            collector.Add(item);
        }

        public class SemanticTextualInfo : TextualInfo
        {
            public IconId? Icon => null; 
            
            public SemanticTextualInfo(HlslSemantic semantic) : base(semantic.Name, semantic.Name)
            {
                Placement.Relevance = (ulong)CppCompletionRanks.Keyword;
            }
        }
    }
}