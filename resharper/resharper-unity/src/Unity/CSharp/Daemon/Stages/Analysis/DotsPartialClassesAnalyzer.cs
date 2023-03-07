#nullable enable
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[]
    {
        typeof(InconsistentModifiersForDotsInheritorWarning),
        typeof(MustBeStructForDotsInheritorWarning),
        typeof(AspectWrongFieldsTypeWarning)
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

            if (typeElement.DerivesFrom(KnownTypes.IAspect))
            {
                foreach (var classMemberDeclaration in classLikeDeclaration.ClassMemberDeclarations)
                {
                    if (classMemberDeclaration is not IFieldDeclaration { Type: IDeclaredType fieldDeclarationType } fieldDeclaration) 
                        continue;
                    var fieldTypeElement = fieldDeclarationType.GetTypeElement();
                    
                    if(fieldTypeElement is not IStruct)
                        continue;
                    
                    if (!fieldTypeElement.DerivesFrom(KnownTypes.IComponentData)) 
                        continue;
                    
                    consumer.AddHighlighting(new AspectWrongFieldsTypeWarning(classLikeDeclaration, fieldDeclaration));
                }
            }
        }

        private static ModifiersProcessingInfo GetModifiersInfo(IClassLikeDeclaration classLikeDeclaration,
            ITypeElement typeElement)
        {
            var parentTypeName = EmptyClrTypeName.Instance;
            var mustBeReadonly = false;
            var mustBeChangedToStruct = false;
            var mustBePartial = false;

            var isClassKeyword = classLikeDeclaration.TypeDeclarationKeyword.NodeType.Equals(CSharpTokenType.CLASS_KEYWORD);
            
            //Base classes are excluded from inheritance check - to avoid extra warnings
            if (CheckInheritance(typeElement, KnownTypes.IAspect))
            {
                mustBeReadonly = !classLikeDeclaration.IsReadonly;
                parentTypeName = KnownTypes.IAspect;
                mustBeChangedToStruct = isClassKeyword;
                mustBePartial = !classLikeDeclaration.IsPartial;
            }
            // TODO: temporary disabled due to upcoming Unity API changes right on RTM 2023.1 release
            else if (CheckInheritance(typeElement, KnownTypes.ISystem))
            {
                parentTypeName = KnownTypes.ISystem;
                mustBeChangedToStruct = isClassKeyword;
                // mustBePartial = !classLikeDeclaration.IsPartial;
            }
            // // ComponentSystemBase is a direct parent for different internal Unity systems, there is a possibility to use for user systems as well
            // // SystemBase : ComponentSystemBase - is widely used as base class for user systems
            // else if (CheckInheritance(typeElement, KnownTypes.ComponentSystemBase) &&
            //          !typeElement.IsClrName(KnownTypes.SystemBase))
            // {
            //     parentTypeName = KnownTypes.SystemBase;
            //     mustBePartial = !classLikeDeclaration.IsPartial;
            // }
            // else if (CheckInheritance(typeElement, KnownTypes.IComponentData))
            // {
            //     parentTypeName = KnownTypes.IComponentData;
            //     mustBeChangedToStruct = isClassKeyword;
            // }

            return new ModifiersProcessingInfo(parentTypeName, mustBeReadonly, mustBePartial, mustBeChangedToStruct);
        }

        private static bool CheckInheritance(ITypeElement typeElement, IClrTypeName baseTypeName)
        {
            return typeElement.DerivesFrom(baseTypeName) && !typeElement.IsClrName(baseTypeName);
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