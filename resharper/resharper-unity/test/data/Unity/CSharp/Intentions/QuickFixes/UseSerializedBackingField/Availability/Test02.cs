using UnityEngine;

public class Base : MonoBehaviour
{
    public virtual int Prop { get; set; }
}

public class Derived : Base
{
    public override int Pr{caret}op { get; }
}