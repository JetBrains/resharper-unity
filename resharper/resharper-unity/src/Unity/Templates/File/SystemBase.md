---
guid: 9C5472EF-F807-4BB4-8E39-56D5B38C96B5
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=System, ValidateFileName=True
scopes: UnityDotsScope
uitag: DOTS
parameterOrder: HEADER, (SYSTEM), (NAMESPACE)
HEADER-expression: fileheader()
SYSTEM-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
---

# SystemBase

```
$HEADER$namespace $NAMESPACE$ {
  public partial class $SYSTEM$ : Unity.Entities.SystemBase
  {
    protected override void OnUpdate()
    {
      $END$
    }
  }
}
```
