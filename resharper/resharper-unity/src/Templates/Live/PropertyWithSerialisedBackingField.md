---
guid: 4d369aa2-16a1-49a5-b2b9-5a7c96cf8ab6
type: Live
reformat: True
shortenReferences: True
categories: unity
scopes: InCSharpTypeMember(minimumLanguageVersion=2.0);MustBeInUnityType
parameterOrder: type#1, propertyName, fieldName
propertyName-expression: suggestVariableName()
fieldName-expression: decapitalize(propertyName)
---

# sprop

Unity property with serialized backing field

```
[UnityEngine.SerializeField] private $type$ $fieldName$;

public $type$ $propertyName$ { get { return this.$fieldName$; } }$END$
```