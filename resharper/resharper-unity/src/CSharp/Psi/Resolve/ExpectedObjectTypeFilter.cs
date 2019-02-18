using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public enum ExpectedObjectTypeReferenceKind
    {
        None,
        Component,
        ScriptableObject
    }

    public class ExpectedObjectTypeFilter : SimpleSymbolFilter
    {
        private readonly ExpectedObjectTypeReferenceKind myKind;
        private readonly bool myMustBeClass;
        private bool myIsBuiltin;

        public ExpectedObjectTypeFilter(ExpectedObjectTypeReferenceKind kind, bool mustBeClass)
        {
            Assertion.Assert(kind != ExpectedObjectTypeReferenceKind.None, "kind != ExpectedObjectTypeReferenceKind.None");
            myKind = kind;
            myMustBeClass = mustBeClass;
        }

        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            var typeElement = declaredElement as ITypeElement;
            if (typeElement == null)
                return !myMustBeClass;

            if (myKind == ExpectedObjectTypeReferenceKind.Component)
            {
                // User components must derive from MonoBehaviour, but built in components only have to derive from
                // Component. A built in component will be something that isn't an asset, which means it's come from
                // one of the UnityEngine assemblies, or UnityEditor.dll. Another check might be that the referenced
                // module lives in Assets, but packages makes a mess of that (referenced packages are compiled and
                // referenced from Library or local packages can include a dll as an asset, external to the project)
                myIsBuiltin = typeElement.IsBuiltInUnityClass();
                if (myIsBuiltin)
                    return typeElement.DerivesFrom(KnownTypes.Component);

                return typeElement.DerivesFrom(KnownTypes.MonoBehaviour);
            }

            return typeElement.DerivesFrom(KnownTypes.ScriptableObject);
        }

        public override ResolveErrorType ErrorType
        {
            get
            {
                switch (myKind)
                {
                    case ExpectedObjectTypeReferenceKind.Component:
                        return myIsBuiltin
                            ? UnityResolveErrorType.EXPECTED_COMPONENT
                            : UnityResolveErrorType.EXPECTED_MONO_BEHAVIOUR;
                    case ExpectedObjectTypeReferenceKind.ScriptableObject:
                        return UnityResolveErrorType.EXPECTED_SCRIPTABLE_OBJECT;
                    default:
                        return ResolveErrorType.IGNORABLE;
                }
            }
        }
    }
}