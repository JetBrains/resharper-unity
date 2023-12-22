---
guid: 276DC9DD-51D2-41F9-A312-D9BD2AE3C225
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=Aspect, ValidateFileName=True
scopes: UnityDotsScope
uitag: DOTS
parameterOrder: HEADER, (ASPECT), (NAMESPACE)
HEADER-expression: fileheader()
ASPECT-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
---

# IAspect

```
$HEADER$namespace $NAMESPACE$ {
  public readonly partial struct $ASPECT$ : Unity.Entities.IAspect
  {
    $END$
  }
}
```
