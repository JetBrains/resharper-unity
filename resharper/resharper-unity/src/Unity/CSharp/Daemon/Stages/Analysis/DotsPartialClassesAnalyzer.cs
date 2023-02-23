#nullable enable
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[]
    {
        typeof(InconsistentModifiersForDotsInheritorWarning),
        typeof(MustBeStructForDotsInheritorWarning)
    })]
    public class DotsPartialClassesAnalyzer : UnityElementProblemAnalyzer<IClassLikeDeclaration>
    {
        public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data)
        {
            return base.ShouldRun(file, data)
                   && DotsUtils.IsUnityProjectWithEntitiesPackage(file);
        }

        public DotsPartialClassesAnalyzer(UnityApi unityApi) : base(unityApi)
        {
        }

        protected override void Analyze(IClassLikeDeclaration classLikeDeclaration, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var typeElement = classLikeDeclaration.DeclaredElement;
            if (typeElement == null)
                return;

            var modifiersProcessingInfo = GetModifiersInfo(classLikeDeclaration, typeElement);

            if (modifiersProcessingInfo.ShouldFixPartialReadonly)
            {
                consumer.AddHighlighting(new InconsistentModifiersForDotsInheritorWarning(classLikeDeclaration,
                    modifiersProcessingInfo.SuperTypeName.ShortName, modifiersProcessingInfo.MustBeMarkedAsPartial,
                    modifiersProcessingInfo.MustBeMarkedAsReadonly));
            }

            if (modifiersProcessingInfo.MustBeChangedToStruct)
            {
                consumer.AddHighlighting(new MustBeStructForDotsInheritorWarning(classLikeDeclaration,
                    modifiersProcessingInfo.SuperTypeName.ShortName));
            }
        }

        private static ModifiersProcessingInfo GetModifiersInfo(IClassLikeDeclaration classLikeDeclaration,
            ITypeElement typeElement)
        {
            var parentTypeName = EmptyClrTypeName.Instance;
            var mustBeReadonly = false;
            var mustBeChangedToStruct = false;
            var mustBePartial = false;

            var isClassKeyword =
                classLikeDeclaration.TypeDeclarationKeyword.NodeType.Equals(CSharpTokenType.CLASS_KEYWORD);

            if (UnityApi.IsDerivesFromIAspect(typeElement))
            {
                mustBeReadonly = !classLikeDeclaration.IsReadonly;
                parentTypeName = KnownTypes.IAspect;
                mustBeChangedToStruct = isClassKeyword;
                mustBePartial = !classLikeDeclaration.IsPartial;
            }
            else if (UnityApi.IsDerivesFromISystem(typeElement))
            {
                parentTypeName = KnownTypes.ISystem;
                mustBeChangedToStruct = isClassKeyword;
                mustBePartial = !classLikeDeclaration.IsPartial;
            }
            else if (UnityApi.IsDerivesFromSystemBase(typeElement))
            {
                parentTypeName = KnownTypes.SystemBase;
                mustBePartial = !classLikeDeclaration.IsPartial;
            }
            else if (UnityApi.IsDerivesFromIComponentData(typeElement))
            {
                parentTypeName = KnownTypes.IComponentData;
                mustBeChangedToStruct = isClassKeyword;
            }

            return new ModifiersProcessingInfo(parentTypeName, mustBeReadonly, mustBePartial, mustBeChangedToStruct);
        }

        private readonly struct ModifiersProcessingInfo
        {
            public readonly IClrTypeName SuperTypeName;
            public readonly bool MustBeMarkedAsReadonly;
            public readonly bool MustBeMarkedAsPartial;
            public readonly bool MustBeChangedToStruct;

            public ModifiersProcessingInfo(IClrTypeName superTypeName, bool mustBeMarkedAsReadonly,
                bool mustBeMarkedAsPartial, bool mustBeChangedToStruct)
            {
                SuperTypeName = superTypeName;
                MustBeMarkedAsReadonly = mustBeMarkedAsReadonly;
                MustBeMarkedAsPartial = mustBeMarkedAsPartial;
                MustBeChangedToStruct = mustBeChangedToStruct;
            }

            public bool ShouldFixPartialReadonly => !string.IsNullOrEmpty(SuperTypeName.ShortName) &&
                                                    (MustBeMarkedAsReadonly || MustBeMarkedAsPartial);

            public bool ShouldClassKeyWord => !string.IsNullOrEmpty(SuperTypeName.ShortName) && MustBeChangedToStruct;
        }
    }
}