using System;
using UnityEngine;
using UnityEngine.Events;

public class NewBehaviourScript : MonoBehaviour
{
    public UnityEvent voidEvent;
    public CustomEvent customEvent;
    public IndirectCustomEvent indirectCustomEvent;

    public void VoidHandler() {
	}

    public void IntHandler(int value) {
    }

    public void FloatHandler(float value) {
    }

    public void BoolHandler(bool value) {
    }

    public void ObjectHandler(UnityEngine.Object value) {
    }

    public void UnityEventHandler(int value1, float value2) {
    }

    public int PropertyEventHandler { get; set; }

    // Do not change the formatting of this
    // This tests that the property gets the annotations, not the setter
    public int PropertyEventHandler2
    {
        get;
        set;
    }
}

[Serializable]
public class CustomEvent : UnityEvent<int, float> {
}

[Serializable]
public class IndirectCustomEvent : CustomEvent {
}
