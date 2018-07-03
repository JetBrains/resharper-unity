---
guid: 7b7fa2c7-0ee5-4d4f-bb1f-ddbeacdbfc94
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=EditModeTest, ValidateFileName=True
scopes: InUnityCSharpEditorFolder;InUnityCSharpFirstpassEditorFolder
parameterOrder: HEADER, (CLASS), (NAMESPACE)
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension()
NAMESPACE-expression: fileDefaultNamespace()
---

# Edit Mode Test

```
$HEADER$using UnityEditor;

namespace $NAMESPACE$ {
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

