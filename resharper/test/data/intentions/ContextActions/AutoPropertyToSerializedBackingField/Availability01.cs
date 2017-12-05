using System;
using UnityEngine;

public class Foo : MonoBehaviour
{
    public int MyAuto{on}Prop1 { get; set; }

    private int _myProp1;
    public int My{off}Prop1
    {
        get { return _myProp1; }
        set { _myProp1 = value; }
    }
}

public interface IFoo
{
    int My{off}Prop1 { get; set; }
}

public class Foo2
{
    public int MyAuto{off}Prop1 { get; set; }
}
