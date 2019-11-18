using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public class EventHandlerSymbolFilter : SimpleSymbolFilter
    {
        private readonly EventHandlerArgumentMode myMode;
        private readonly FrugalLocalList<IType> myTypes;

        public EventHandlerSymbolFilter(EventHandlerArgumentMode mode, string type, IPsiModule psiModule)
        {
            myTypes = new FrugalLocalList<IType>();

            myMode = mode;
            if (mode == EventHandlerArgumentMode.UnityObject && type != null)
            {
                myTypes.Add(TypeFactory.CreateTypeByCLRName(type, psiModule));
            }
            else if (mode == EventHandlerArgumentMode.EventDefined && type != null)
            {
                var eventType = TypeFactory.CreateTypeByCLRName(type, psiModule);
                var unityEventType = GetUnityEventType(eventType);
                var unityEventTypeElement = unityEventType?.GetTypeElement();
                if (unityEventType != null && unityEventTypeElement != null)
                {
                    var (_, substitution) = unityEventType.Resolve();
                    var typeParameters = unityEventTypeElement.TypeParameters;
                    foreach (var t in typeParameters)
                    {
                        myTypes.Add(substitution.Apply(t));
                    }
                }
            }
            else if (mode != EventHandlerArgumentMode.Void)
            {
                var predefinedTypes = psiModule.GetPredefinedType();
                switch (mode)
                {
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

        public override ResolveErrorType ErrorType => ResolveErrorType.ARGUMENTS_MISMATCH;

        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            if (myMode != EventHandlerArgumentMode.Void && myTypes.Count == 0)
                return false;

            if (!(declaredElement is IMethod method))
                return false;

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
    }
}