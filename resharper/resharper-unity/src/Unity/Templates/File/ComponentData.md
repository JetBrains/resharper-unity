---
guid: B0D13301-16C0-4271-B12A-4CDB03708E76
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=ComponentData, ValidateFileName=True
scopes: UnityDotsScope
uitag: DOTS
parameterOrder: HEADER, (COMPONENT), (NAMESPACE)
HEADER-expression: fileheader()
COMPONENT-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
---

# IComponentData

```
$HEADER$namespace $NAMESPACE$ {
  public struct $COMPONENT$ : Unity.Entities.IComponentData
  {
    $END$
  }
}
```
