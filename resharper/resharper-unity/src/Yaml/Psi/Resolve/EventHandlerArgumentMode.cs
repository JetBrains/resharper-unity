namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    // Corresponds to UnityEngine.Events.PersistentListenerMode
    // EventDefined means the handler receives the arguments from the event itself, i.e. UnityEvent (void) or
    // UnityEvent<T, ...> and the handler's parameters must match this signature.
    // The other values are static values set in the Inspector. The Inspector will list all possible event handlers
    // and the single method parameter defines the argument mode.
    // The Inspector will only list public methods with one parameter and a void return type.
    // Static methods are listed, but described in the Inspector as "missing" and are not invoked.
    // Private methods are not listed, but are invoked.
    // Methods with return types are not listed, and are not invoked (there is a type mismatch exception at runtime).
    // Methods with too many parameters are marked as "missing" and not invoked.
    public enum EventHandlerArgumentMode
    {
        EventDefined = 0,
        Void = 1,
        UnityObject = 2,
        Int = 3,
        Float = 4,
        String = 5,
        Bool = 6
    }
}