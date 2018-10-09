using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public class UnityObjectTypeReferenceFactory : StringLiteralReferenceFactoryBase
    {
        public override bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            if (element is ILiteralExpression literal && literal.ConstantValue.IsString())
            {
                // Note that this is case insensitive. I don't think it really matters, as resolving will handle case
                // insensitivity correctly
                return names.HasAnyNameIn((string) literal.ConstantValue.Value);
            }

            return false;
        }

        public override ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            var literal = GetValidStringLiteralExpression(element);
            if (literal == null)
                return ReferenceCollection.Empty;

            // GameObject.AddComponent, GameObject.GetComponent and ScriptableObject.CreateInstance all have the string
            // literal as the first argument
            if (!IsFirstArgumentInMethod(literal))
                return ReferenceCollection.Empty;

            var invocationExpression = literal.GetContainingNode<IInvocationExpression>();
            var invocationReference = invocationExpression?.Reference;
            if (!(invocationReference?.Resolve().DeclaredElement is IMethod invokedMethod))
                return ReferenceCollection.Empty;

            var kind = GetExpectedReferenceKind(invokedMethod);
            var newReferences = kind == ExpectedObjectTypeReferenceKind.None
                ? ReferenceCollection.Empty
                : CreateTypeNameReferences(literal, kind);

            return ResolveUtil.ReferenceSetsAreEqual(newReferences, oldReferences) ? oldReferences : newReferences;
        }

        private static bool IsFirstArgumentInMethod(ILiteralExpression literal)
        {
            var argument = CSharpArgumentNavigator.GetByValue(literal as ICSharpExpression);
            var argumentsOwner = CSharpArgumentsOwnerNavigator.GetByArgument(argument);
            return argumentsOwner != null && argumentsOwner.ArgumentsEnumerable.FirstOrDefault() == argument;
        }

        private ExpectedObjectTypeReferenceKind GetExpectedReferenceKind(IMethod invokedMethod)
        {
            var name = invokedMethod.ShortName;
            if (name == "GetComponent" && (DoesMethodBelongToType(invokedMethod, KnownTypes.GameObject) ||
                                           DoesMethodBelongToType(invokedMethod, KnownTypes.Component)))
            {
                return ExpectedObjectTypeReferenceKind.Component;
            }

            if (name == "AddComponent" && DoesMethodBelongToType(invokedMethod, KnownTypes.GameObject))
            {
                // Note that GameObject.AddComponent(string) is obsolete. Seems to have become obsolete in 5.5, and the
                // obsolete attribute has the error flag set to true (at least for 2018.2). It also looks like Unity's
                // API upgrader will automatically rewrite this to GameObject.AddComponent<T>(). So, this is really
                // only useful for Unity 5.4 and below
                return ExpectedObjectTypeReferenceKind.Component;
            }

            if (name == "CreateInstance" && DoesMethodBelongToType(invokedMethod, KnownTypes.ScriptableObject))
                return ExpectedObjectTypeReferenceKind.ScriptableObject;
            return ExpectedObjectTypeReferenceKind.None;
        }

        private bool DoesMethodBelongToType(IMethod invokedMethod, IClrTypeName typeName)
        {
            var containingType = invokedMethod.GetContainingType();
            return containingType != null && Equals(containingType.GetClrName(), typeName);
        }

        private ReferenceCollection CreateTypeNameReferences(ICSharpLiteralExpression literal,
                                                             ExpectedObjectTypeReferenceKind kind)
        {
            var literalValue = (string) literal.ConstantValue.Value;
            if (literalValue == null)
                return ReferenceCollection.Empty;

            var symbolCache = literal.GetPsiServices().Symbols;

            IQualifier qualifier = null;
            var references = new LocalList<IReference>();
            var startIndex = 0;
            var nextDotIndex = literalValue.IndexOf('.');
            while (true)
            {
                var endIndex = nextDotIndex != -1 ? nextDotIndex : literalValue.Length;

                // startIndex + 1 to skip leading quote in tree node, which doesn't exist in literalValue
                var rangeWithin = TextRange.FromLength(startIndex + 1, endIndex - startIndex);

                // Behaviour and resolution is almost identical for each part.
                // For a single component, it is either a Unity object with an inferred namespace, a type in the global
                // namespace, or the namespace for an as yet uncompleted qualified type name
                // For a trailing component, it is a qualified reference, and could be a type, or a continuation of the
                // namespace qualification
                // For a middle component, it could be a namespace, or the user typing a new type, with the trailing
                // text being the old component
                // When there is no qualifier, resolve should match:
                // * inferred type, with expected type check
                // * type in global namespace, with expected type check
                // * namespace, with expected type check (so namespace won't be the last thing)
                // When there is a qualifier, resolve should match
                // * namespaces
                // * qualified type with expected type check
                // For the final component, resolve should match namespaces, but with the expected type check
                // At all times, completion should show both namespaces and qualified types
                // Leading and trailing space are treated as part of a name, and will cause resolve to fail
                // TODO: Handle trailing dot
                var isFinalPart = nextDotIndex == -1;
                var reference = new UnityObjectTypeOrNamespaceReference(literal, qualifier, literal.Literal, rangeWithin,
                    kind, symbolCache, isFinalPart);

                references.Add(reference);
                if (nextDotIndex == -1)
                    break;

                startIndex = nextDotIndex + 1;
                nextDotIndex = literalValue.IndexOf('.', startIndex);
                qualifier = reference;
            }

            return new ReferenceCollection(references.ReadOnlyList());
        }
    }
}