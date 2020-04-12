using UnityEngine;
using JetBrains.Annotations;

public class Base : MonoBehaviour
{
  private void OnAudioFilterRead()
  {
  }
}

public class Derived : Base
{
  OnAudioFilter{caret}
}
