using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    // This filter is the equivalent of UnityEventBase.FindMethod
    // https://github.com/Unity-Technologies/UnityCsReference/blob/2018.4/Runtime/Export/UnityEvent.cs#L721
    // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.3/Runtime/Export/UnityEvent/UnityEvent.cs#L721
    //
    // Essentially, find a public or non-public instance method, anywhere in the hierarchy, with parameters that match
    // the requested arguments
    public class EventHandlerSymbolFilter : SimpleSymbolFilter
    {
        private readonly EventHandlerArgumentMode myMode;
        private readonly FrugalLocalList<IType> myTypes;

        public EventHandlerSymbolFilter(EventHandlerArgumentMode mode, string type, IPsiModule psiModule)
        {
            myTypes = new FrugalLocalList<IType>();

            myMode = mode;
            var predefinedTypes = psiModule.GetPredefinedType();
            switch (mode)
            {
                case EventHandlerArgumentMode.EventDefined:
                    if (type != null)
                    {
                        // Find the UnityEvent base type, and use the type parameters as the required arguments
                        // This only works for scenes serialised in Unity 2018.3 and below. The field was removed in 2018.4
                        var eventType = TypeFactory.CreateTypeByCLRName(type, psiModule);
                        var unityEventType = GetUnityEventType(eventType);
                        var unityEventTypeElement = unityEventType?.GetTypeElement();
                        if (unityEventType != null && unityEventTypeElement != null)
                        {
                            var (_, substitution) = unityEventType.Resolve();
                            var typeParameters = unityEventTypeElement.TypeParameters;
                            foreach (var t in typeParameters)
                                myTypes.Add(substitution.Apply(t));
                        }
                    }
                    break;

                case EventHandlerArgumentMode.UnityObject:
                    myTypes.Add(type == null
                        ? TypeFactory.CreateTypeByCLRName(KnownTypes.Object, NullableAnnotation.Unknown, psiModule)
                        : TypeFactory.CreateTypeByCLRName(type, psiModule));
                    break;

                case EventHandlerArgumentMode.Int:
                    myTypes.Add(predefinedTypes.Int);
                    break;

                case EventHandlerArgumentMode.Float:
                    myTypes.Add(predefinedTypes.Float);
                    break;

                case EventHandlerArgumentMode.String:
                    myTypes.Add(predefinedTypes.String);
                    break;

                case EventHandlerArgumentMode.Bool:
                    myTypes.Add(predefinedTypes.Bool);
                    break;
            }
        }

        public override ResolveErrorType ErrorType => ResolveErrorType.ARGUMENTS_MISMATCH;

        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            if (myMode != EventHandlerArgumentMode.Void && myMode != EventHandlerArgumentMode.EventDefined && myTypes.Count == 0)
                return false;

            if (!(declaredElement is IMethod method))
                return false;

            // Since Unity 2018.4+ we don't know the type of the event
            if (myMode == EventHandlerArgumentMode.EventDefined && myTypes.Count == 0)
                return true;

            var parameters = method.Parameters;
            if (parameters.Count == myTypes.Count)
            {
                for (var i = 0; i < myTypes.Count; i++)
                {
                    if (!parameters[i].Type.Equals(myTypes[i]))
                        return false;
                }

                return true;
            }

            return false;
        }

        [CanBeNull]
        private static IDeclaredType GetUnityEventType(IDeclaredType eventType)
        {
            foreach (var superType in eventType.GetAllSuperTypes())
            {
                if (superType.GetClrName().ShortName == "UnityEvent")
                    return superType;
            }

            return null;
        }
    }
}