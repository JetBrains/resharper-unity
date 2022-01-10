using UnityEditor;
using UnityEngine;

[InitializeOn{caret}Load]
public class MissingConstructor()
{
  private int myField;

  public String MyProperty { get; set; }
}
