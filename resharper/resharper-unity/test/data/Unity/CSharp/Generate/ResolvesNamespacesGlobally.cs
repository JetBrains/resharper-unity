// ${KIND:Unity.EventFunctions}
// ${SELECT0:OnJointBreak(System.Single):System.Void}
using UnityEngine;

// This can cause types in the System namespace to resolve incorrectly
// E.g. void OnJointBreak could be generated as Void OnJointBreak(Single) { ... }
namespace Foo.System.Bar
{
  class A : MonoBehaviour
  {
    {caret}
  }
}
