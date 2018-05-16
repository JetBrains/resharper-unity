---
guid: 0bcdbc13-d26e-4512-9750-fb930f532e88
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=PlayModeTest, ValidateFileName=True
scopes: InUnityCSharpAssetsFolder
parameterOrder: HEADER, (CLASS), (NAMESPACE)
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension()
NAMESPACE-expression: fileDefaultNamespace()
---

# Play Mode Test

```
$HEADER$namespace $NAMESPACE$ {
  public class $CLASS$ {
    [NUnit.Framework.Test]
    public void $CLASS$SimplePasses() {
      // Use the Assert class to test conditions.
      $END$
    }
    
    // A UnityTest behaves like a coroutine in PlayMode
    // and allows you to yield null to skip a frame in EditMode
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator $CLASS$WithEnueratorPasses() {
      // Use the Assert class to test conditions.
      // yield to skip a frame
      yield return null;
    }
  }
}
```
