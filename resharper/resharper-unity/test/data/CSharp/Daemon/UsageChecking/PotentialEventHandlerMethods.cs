using System;
using UnityEngine;

// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable ConvertToAutoProperty
public class A : MonoBehaviour
{
  private string myThing;
  private static string ourThing;

  public void PotentialEventHandler()
  {
  }

  public void PotentialEventHandler(string s, int i)
  {
  }

  public string SetterPotentialEventHandler
  {
      get { return myThing; }
      set { myThing = value; }
  }

  private void WrongAccessibility()
  {
  }

  public string WrongReturnType()
  {
      return "Hello";
  }

  public static void WrongStatic()
  {
  }

  public static string WrongStaticProperty
  {
      get { return ourThing; }
      set { ourThing = value; }
  }

  [Obsolete]
  public void ObsoleteNotConsidered()
  {
  }
}
