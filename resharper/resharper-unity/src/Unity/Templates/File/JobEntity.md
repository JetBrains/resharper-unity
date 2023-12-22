---
guid: 60A9DD1A-9237-49A8-BFAA-D8510DF7FAE0
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=JobEntity, ValidateFileName=True
scopes: UnityDotsScope
uitag: DOTS
parameterOrder: HEADER, (JOBENTITY), (NAMESPACE)
HEADER-expression: fileheader()
JOBENTITY-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
---

# IJobEntity

```
$HEADER$namespace $NAMESPACE$ 
{
  public partial struct $JOBENTITY$ : Unity.Entities.IJobEntity 
  {
     public void Execute($END$)
     {

     }
  }
}
```
