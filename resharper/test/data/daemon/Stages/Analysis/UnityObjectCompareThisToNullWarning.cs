using System;
using UnityEngine;

public class MyMonoBehaviour : MonoBehaviour
{
    public void CompareToNullWithCustomEqualityOperator()
    {
        if (this == null)
        {
            Console.WriteLine();
        }
    }

    public void CompareToNullWithCustomInequalityOperator()
    {
        if (this != null)
        {
            Console.WriteLine();
        }
    }
}

public class PlainOldClass
{
    public void CompareToNullWithStandardEqualityOperator()
    {
        if (this == null)
        {
            Console.WriteLine();
        }
    }

    public void CompareToNullWithStandardInequalityOperator()
    {
        if (this != null)
        {
            Console.WriteLine();
        }
    }
}
