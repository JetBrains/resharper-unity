using UnityEngine;

public class UnityObjectEqualitySuspiciousComparisonWarning : MonoBehaviour
{
    public void TestMethod(Component component, Component component2)
    {
        var rigidBody = GetComponent<Rigidbody>();
        var collider = GetComponent<Collider>();
        
        // No warning
        if (component == component2) { }

        // Suspicious comparison warnings
        if (this == rigidBody) { }
        if (this != rigidBody) { }

        // Suspicious comparison warnings
        if (collider == rigidBody) { }
        if (collider != rigidBody) { }
        
        // No warnings
        if (collider == component) { }
        if (rigidBody == component) { }
    }

    public void TestMethod2(BaseBehaviour baseBehaviour, DerivedBehaviour derivedBehaviour)
    {
        // Suspicious comparison warnings
        if (baseBehaviour == this) { }
        if (baseBehaviour != this) { }
        
        // No warnings
        if (baseBehaviour == derivedBehaviour) { }
    }
}

public class BaseBehaviour : MonoBehaviour
{
}

public class DerivedBehaviour : BaseBehaviour
{
}
