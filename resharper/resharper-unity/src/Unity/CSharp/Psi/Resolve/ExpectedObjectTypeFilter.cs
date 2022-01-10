using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

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
                return typeElement.IsUnityComponent(out myIsBuiltin);

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