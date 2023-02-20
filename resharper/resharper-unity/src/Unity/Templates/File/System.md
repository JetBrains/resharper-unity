---
guid: 3A8DDF9A-86ED-4877-8721-6D064DF61D77
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

# ISystem

```
$HEADER$namespace $NAMESPACE$ {
  public partial struct $SYSTEM$ : Unity.Entities.ISystem
  {
    public void OnCreate(ref Unity.Entities.SystemState state)
    {
        $END$
    }

    public void OnDestroy(ref Unity.Entities.SystemState state)
    {
    }

    public void OnUpdate(ref Unity.Entities.SystemState state)
    {
    }
  }
}
```
