using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IAttribute), HighlightingTypes = new[] { typeof(RedundantAttributeOnTargetWarning) })]
    public class RedundantAttributeOnTargetProblemAnalyzer : UnityElementProblemAnalyzer<IAttribute>
    {
        private static readonly Dictionary<IClrTypeName, AttributeTargets> ourAttributeData =
            new Dictionary<IClrTypeName, AttributeTargets>
            {
                // UnityEngine
                {KnownTypes.AddComponentMenu, AttributeTargets.Class},
                {KnownTypes.ExecuteInEditMode, AttributeTargets.Class},
                {KnownTypes.HideInInspector, AttributeTargets.Field},
                // All but undocumented. Appears to have the same usage as ImageEffectOpaque
                // The ImageEffect* attributes that are applied to methods are only useful
                // when applied to OnRenderImage
                // Actually, it's a little too undocumented. There is no indication of how
                // it's applied
                //{KnownTypes.ImageEffectAfterScale, AttributeTargets.Method},
                {KnownTypes.ImageEffectAllowedInSceneView, AttributeTargets.Class},
                {KnownTypes.ImageEffectOpaque, AttributeTargets.Method},
                {KnownTypes.ImageEffectTransformsToLDR, AttributeTargets.Method},
                {KnownTypes.SerializeField, AttributeTargets.Field},

                // UnityEditor
                {KnownTypes.CanEditMultipleObjects, AttributeTargets.Class},
                {KnownTypes.CustomEditor, AttributeTargets.Class },
                {KnownTypes.DrawGizmo, AttributeTargets.Method},

                // UnityEditor.Callbacks
                {KnownTypes.DidReloadScripts, AttributeTargets.Method},
                {KnownTypes.OnOpenAssetAttribute, AttributeTargets.Method},
                {KnownTypes.PostProcessBuildAttribute, AttributeTargets.Method},
                {KnownTypes.PostProcessSceneAttribute, AttributeTargets.Method},
            };

        public RedundantAttributeOnTargetProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IAttribute element, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (!(element.TypeReference?.Resolve().DeclaredElement is ITypeElement attributeTypeElement))
                return;

            if (ourAttributeData.TryGetValue(attributeTypeElement.GetClrName(), out var validTargets))
            {
                ITreeNode declaration = element.GetContainingNode<IMultipleDeclaration>() ??
                                        (ITreeNode) element.GetContainingNode<IDeclaration>();
                var declarationType = GetDeclarationType(element, declaration);
                if (!validTargets.HasFlag(declarationType))
                {
                    consumer.AddHighlighting(
                        new RedundantAttributeOnTargetWarning(element, attributeTypeElement, validTargets));
                }
            }
        }

        private static AttributeTargets GetDeclarationType(IAttribute attribute, ITreeNode declaration)
        {
            switch (declaration)
            {
                case IMultipleEventDeclaration _:
                    return attribute.Target == AttributeTarget.Field ? AttributeTargets.Field : AttributeTargets.Event;
                case IMultipleFieldDeclaration _: return AttributeTargets.Field;
                case IMultipleConstantDeclaration _: return AttributeTargets.Field;
                case IClassDeclaration _: return AttributeTargets.Class;
                case IStructDeclaration _: return AttributeTargets.Struct;
                case IEnumDeclaration _: return AttributeTargets.Enum;
                case IConstructorDeclaration _: return AttributeTargets.Constructor;
                case IDelegateDeclaration _: return AttributeTargets.Delegate;
                case IMethodDeclaration _:
                    return attribute.Target == AttributeTarget.Return
                        ? AttributeTargets.ReturnValue
                        : AttributeTargets.Method;
                case IPropertyDeclaration _: return AttributeTargets.Property;
                case IFieldDeclaration _: return AttributeTargets.Field;
                case IEventDeclaration _: return AttributeTargets.Event;
                case IInterfaceDeclaration _: return AttributeTargets.Interface;
                case IParameterDeclaration _: return AttributeTargets.Parameter;
                case ITypeParameterDeclaration _: return AttributeTargets.GenericParameter;
            }

            return AttributeTargets.Assembly;
        }
    }
}