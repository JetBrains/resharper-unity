# Code Inspection: Possible unintended bypass of lifetime check of underlying Unity engine object

This is a [Unity](https://unity3d.com/) specific inspection. It only runs in a Unity project.

%product% will show this warning if a type deriving from `UnityEngine.Object` uses either the [null coalescing](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-conditional-operator) (`??`) or [null propagation or conditional](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-conditional-operators) (`?.`) operators. These operators do not use the custom equality operators declared on `UnityEngine.Object`, and so bypass a check to see if the underlying native Unity engine object has been destroyed. An explicit `null` or boolean comparison, or a call to `System.Object.ReferenceEquals()` is preferred in order to clarify intent.

## Details

Types deriving from `UnityEngine.Object` are managed .NET objects that are used in C# scripts to represent and work with native Unity engine objects. These two types of objects have different lifetimes. The managed .NET objects are garbage collected when there are no more references to them, while the native Unity engine objects are destroyed when a new scene is loaded or through an explicit call to `UnityEngine.Object.Destroy()`. This means it is possible to have a managed .NET object that refers to a destroyed native object.

The `UnityEngine.Object` class defines custom equality operators that will check to see if the underlying native Unity engine object has been destroyed, when being compared against `null`. In other words, `myMonoBehaviour == null` will check to see if the `myMonoBehaviour` variable has been assigned, and will also see if the native engine object has been destroyed. The same check can be performed with a boolean comparison, such as `if (myMonoBehaviour == true)` or `if (!myMonoBehaviour)` or just `if (myMonoBehaviour)`.

Intent is ambiguous if the null coalescing or conditional operators are used, and it is possible that an intended lifetime check has been bypassed. If a lifetime check was intended, prefer an explicit comparison against `null`, or a boolean comparison. If a lifetime check was not intended, be explicit about intent with a call to `System.Object.ReferenceEquals()`. Note that a call to `object.ReferenceEquals()` is optimised to a simple `null` check by the compiler, and is quicker than calling the custom equality operators.

## Null Coalescing operator

In the following example, the intent is not clear. Is this a check to see if the `gameObject` variable has been correctly assigned, or to check that the native Unity engine object has been destroyed?

```csharp
var go = gameObject ?? CreateNewGameObject();
```

If the intent was to check the lifetime of the underlying engine object, then this code is incorrect, as the lifetime check has been bypassed. Fix the code with an explicit `null` or boolean comparison:

```csharp
var go = gameObject != null ? gameObject : CreateNewGameObject();

// Or use the implicit bool conversion operators for the same check
go = gameObject ? gameObject : CreateNewGameObject();
```

If the intent was to ensure that the `gameObject` variable has been initialised and assigned a valid C# reference, prefer an explicit call to `object.ReferenceEquals()`:

```csharp
return !object.ReferenceEquasl(gameObject, null) ? gameObject : CreateNewGameObject();
```

While these changes are slightly more verbose, the intent is now clear.

## Null Conditional operator

This example is also ambiguous about intent:

```csharp
monoBehaviour?.Invoke("Attack", 1.0f);
```

Again, if the intent is to simply check that the `monoBehaviour` variable has been correctly initialised and assigned, prefer an explicit call to `object.ReferenceEquals()`:

```csharp
if (!object.ReferenceEquals(monoBehaviour, null))
  monoBehaviour.Invoke(\"Attack\", 1.0f);
```

But if the intent was to check the lifetime of the underlying engine object, prefer an explicit `null` or boolean comparison:

```csharp
if (monoBehaviour != null)
  monoBehaviour.Invoke("Attack", 1.0f);

// Or use the implicit bool conversion operators
if (otherBehaviour)
  otherBehaviour.Invoke("Attack", 1.0f);
```

## See also

More details on this topic can be found in the Unity blog post ["Custom == operator, should we keep it?"](https://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/).

