using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    public void Start()
    {
    }

    public void Update()
    {
        // With a comment
    }

    public void FixedUpdate()
    {
#if UNITY_EDITOR
#endif
    }

    public void LateUpdate()
    {
        int i;
    }
}

public class Base : MonoBehaviour
{
    public virtual void Update()
    {
        Console.WriteLine("Doing something");
    }

    public virtual void FixedUpdate()
    {
        Console.WriteLine("Doing something");
    }

    // Not redundant - has derived symbol and removing this will break code
    public virtual void LateUpdate()
    {
    }

    public void Start()
    {
        Console.WriteLine("Doing something");
    }

    public virtual void Reset()
    {
        Console.WriteLine("Doing something");
    }

    // Redundant - empty method, not inherited
    public virtual void OnValidate()
    {
    }
}

public class Derived : Base
{
    // Not redundant - hides base implementation
    public override void Update()
    {
    }

    // Not redundant - doing something
    public override void FixedUpdate()
    {
        Console.WriteLine("Doing something");
    }

    // Not redundant - doing something. Also overrides empty virtual method in Base
    public override void LateUpdate()
    {
        Console.WriteLine("Doing something");
    }

    // Not redundant - shadows base implementation
    public new void Start()
    {
    }

    // Normal redundant override warning
    public override void Reset()
    {
        base.Reset();
    }
}