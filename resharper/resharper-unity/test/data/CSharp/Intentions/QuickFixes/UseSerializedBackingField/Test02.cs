using UnityEngine;

public abstract class Base : MonoBehaviour
{
    public abstract int Prop { get; set; }
}

public class Derived : Base
{
    public override int Pr{caret}op { get; }
}
