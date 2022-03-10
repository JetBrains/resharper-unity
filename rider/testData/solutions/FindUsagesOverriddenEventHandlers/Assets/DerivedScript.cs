using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DerivedScript : BaseScript
{
    public override void Foo()
    {
        Foo();
    }
}
