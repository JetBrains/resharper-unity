using System;
using UnityEngine;

public class MyMonoBehaviour : MonoBehaviour
{
  public const int EeeeeField = 42;
  private string QqqqqField;
  public string BbbbbField; // Serialised field
  public readonly int CccccField;
  public static readonly int Ccccc2Field;
  [Header("Something")]
  [SerializeField] private int YyyyyyField; // Serialised field
  public string AaaaaField; // Serialised field
  public static int DdddField;
  public void WwwwwMethod()
  {
  }
  [SerializeField] private int ZzzzzField; // Serialised field
  public const int OooooField = 42;
  [Header("Something else")]
  public string AaaaaField2; // Serialised field
  public enum MyEnum
  {
    One,
    Two,
    Three
  }

  public void TtttMethod()
  {
  }

  public void FixedUpdate() // Event function
  {
  }

  [field: SerializeField] public int MyAutoPropertyWithSerializedBackingField;

  public void AaaaaMethod()
  {
  }

  public void Update() // Event function
  {
  }

  public void WwwwMethod2()
  {
  }

  public void OnApplicationPause(bool pauseStatus) // Event function
  {
  }

  public void LateUpdate() // Event function
  {
  }

  private void OnApplicationFocus(bool hasFocus) // Event function
  {
  }
}

// Normal class. Methods are not reordered
public class NonUnityType
{
  public void Update()
  {
  }

  public void OnApplicationPause(bool pauseStatus)
  {
  }

  public void LateUpdate()
  {
  }
}


[Serializable]
public class SerializableClass
{
  public string Bbbbb;
  public string Ccccc;
  public string Aaaaa;
  public readonly string Cccc2;
  private string Aaaaa2;
}

// Normal class. Fields are reordered
public class NonSerializableClass
{
  public string Bbbbb;
  public string Ccccc;
  public string Aaaaa;
  public readonly string Cccc2;
  private string Aaaaa2;
}
