# USS

The valid properties are listed in the `src/main/resources/uss/element-descriptors.xml` file. This file is primarily
compiled from the Unity reference source.

When a `.uss` file is added to a Unity project, it is parsed and imported into an internal asset. This asset is then
validated, and it is this validation that is used to create the `element-descriptors.xml` file.

## Parsing and importing stylesheets

When adding a `.uss` file, Unity will call a stylesheet importer (natively in 2017.1, as a
[scripted imported](https://docs.unity3d.com/Manual/ScriptedImporters.html) in 2017.2 and above). This importer uses
[ExCss](https://github.com/TylerBrinks/ExCSS) to parse the `.uss` files, purely as syntax - the properties aren't
validated, so the file just has to be valid syntactically. The importer will then walk the parsed stylesheet object
model and start to build the `StyleSheet` asset. The property values are mostly stored as primitive values. There is
some conversion from e.g. identifiers to keywords (`inherit`) or to create an actual `Color` instance, or to recognise
functions.
 
All of this happens in the editor. At runtime, the UIElements module uses the `StyleSheet` asset as pre-parsed data to
apply to the visual tree.
 

The stylesheet importer:
* Unity 2017.2 - [`StyleSheetImporter.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.2/Editor/Mono/StyleSheets/StyleSheetImporter.cs)

## 2017.1

* [`StyleSheetImporter.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/StyleSheets/CSSSpec.cs)
* [`VisualElementStyles.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Runtime/UIElements/Managed/StyleSheets/VisualElementStyles.cs)

In 2017.1, the properties are stored in a file called [`VisualElementStyles`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Runtime/UIElements/Managed/StyleSheets/VisualElementStyles.cs).
This class has a number of fields all marked with the `[StyleProperty]` attribute. This attribute contains the property
name and a property ID. The field is of type `Style<T>`, where `T` is the "resolved" type of the property - e.g. `int`
instead of enum and `Texture2D` for images. When applying enums, the string property value has the `-` characters
removed, and the enum value is parsed.

* Only supports `resource()` function.
* Does not support `rgb()` or `rgba()`.
* Supports `inherit` and `unset` for all properties.

## 2017.2

* [`StyleSheetImporter.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.2/Editor/Mono/StyleSheets/StyleSheetImporter.cs)
* [`VisualElementStylesData.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.2/Runtime/UIElements/Managed/StyleSheets/VisualElementStylesData.cs)
* [`StyleSheetCache.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.2/Runtime/UIElements/Managed/StyleSheets/StyleSheetCache.cs)

The list of properties is now kept in a dictionary in `StyleSheetCache.cs`. Other than that, no significant changes.

* Add `border-*-width`

## 2017.3

* [`StyleSheetImporter.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.3/Editor/Mono/StyleSheets/StyleSheetImporter.cs)
* [`VisualElementStylesData.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.3/Runtime/UIElements/Managed/StyleSheets/VisualElementStylesData.cs)
* [`StyleSheetCache.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.3/Runtime/UIElements/Managed/StyleSheets/StyleSheetCache.cs)

Minor property changes.

* Add `border-*-radius` + change `border-radius` to shorthand property

## 2017.4

* [`StyleSheetImporter.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.4/Editor/Mono/StyleSheets/StyleSheetImporter.cs)
* [`VisualElementStylesData.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.4/Runtime/UIElements/Managed/StyleSheets/VisualElementStylesData.cs)
* [`StyleSheetCache.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2017.4/Runtime/UIElements/Managed/StyleSheets/StyleSheetCache.cs)

## 2018.1

* [`StyleSheetImporterImpl.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.1/Editor/Mono/StyleSheets/StyleSheetImporterImpl.cs)
* [`VisualElementStylesData.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.1/Modules/UIElements/StyleSheets/VisualElementStylesData.cs)
* [`StyleSheetCache.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.1/Modules/UIElements/StyleSheets/StyleSheetCache.cs)

Minor property changes.

* Introduces `flex-basis`, `flex-grow` and `flex-shrink`
* Introduces `cursor`

## 2018.2

* [`StyleSheetImporterImpl.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.2/Editor/Mono/StyleSheets/StyleSheetImporterImpl.cs)
* [`VisualElementStylesData.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.2/Modules/UIElements/StyleSheets/VisualElementStylesData.cs)
* [`StyleSheetCache.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.2/Modules/UIElements/StyleSheets/StyleSheetCache.cs)

Minor property changes. Using [Yoga](https://yogalayout.com) for layout.

* Introduces `visibility`

## 2018.3

* [`StyleSheetImporterImpl.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.3/Editor/Mono/StyleSheets/StyleSheetImporterImpl.cs)
* [`VisualElementStylesData.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.3/Modules/UIElements/StyleSheets/VisualElementStylesData.cs)
* [`StyleSheetCache.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.3/Modules/UIElements/StyleSheets/StyleSheetCache.cs)

Adds support for the `url()` function. Accepts URL beginning with `project:`. If URL begins with `/` treat as `project:`
URL, else try to create a C# `Uri` with an absolute path. If that fails, try to create relative to current `.uss` file.
During parsing, declares a dependency on the source asset, loads it and stores the reference, ready to be serialised.

Lots of renames and deprecated names. The deprecated names are silently remapped to the new names.

* Introduces `url()` function
* Introduces `color`
* `flex-wrap` supports new value `wrap-reverse`
* `overflow` is missing the `scroll` value
* `background-image` support `url()` function
* `flex` shorthand applies correctly to `flex-grow`, `flex-shrink` and `flex-basis`
* `flex-basis` data type more complex
* `border-left` et al replaced with `border-*-width`
* `font` replaced with `-unity-font` (data type changed to add `url()`)
* `font-style` replaced with `-unity-font-style`
* Introduces `position`
* `position-type` replaced with `-unity-position-type` (overlaps with `position`?)
* `text-alignment` replaced with `-unity-text-align`
* `text-clipping` replaced with `-unity-clipping`
* `text-color` replaced with `color`
* `word-wrap` replaced with `-unity-word-wrap`
* `background-size` replacedd with `-unity-background-scale-mode`
* Introduces `-unity-background-scale-mode`
* `position-left` et al replaced with `left`, `top`, `right`, `bottom`
* `slice-left` et al replaced with `-unity-slice-*`

## 2018.4

* [`StyleSheetImporterImpl.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.4/Editor/Mono/StyleSheets/StyleSheetImporterImpl.cs)
* [`VisualElementStylesData.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.4/Modules/UIElements/StyleSheets/VisualElementStylesData.cs)
* [`StyleSheetCache.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2018.4/Modules/UIElements/StyleSheets/StyleSheetCache.cs)

No changes

## 2019.1

* [`StyleSheetImporterImpl.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.1/Modules/StyleSheetsEditor/StyleSheetImporterImpl.cs)
* [`VisualElementStylesData.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.1/Modules/UIElements/StyleSheets/VisualElementStylesData.cs)
* [`StyleSheetCache.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.1/Modules/UIElements/StyleSheets/StyleSheetCache.cs)
* [`StyleSheetApplicator`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.1/Modules/UIElements/StyleSheets/StyleSheetApplicator.cs)

Parsing now supports unknown functions. Previously, would throw an error if the function wasn't `resource()` (`url()` is
handled natively by ExCss). Each argument to the function is parsed as a separate term, so added to the `StyleSheet`
asset correctly. ExCss splits the arugments, presumably on comma.

The way of applying the styles has changed, with `VisualElementStylesData.ApplyStyleProperty` delegating to an interface.
That interface has two implementations. The one that pulls the data out of the `StyleSheet` asset is `StyleSheetApplicator`.

Introduces `initial` support. It is handled as a global keyword in `VisualElementStylesData.ApplyRule`. It checks if the
value is `initial` and then applies either initial values or the rule values.

* Introduces `initial` keyword
* Introduces `display` and `white-space` properties
* Introduces `margin` and `padding` shorthand properties
* Introduces `-unity-background-image-tint-color` property
* `background-image` allows both `unset` and `none`
* Shorthand properties are better supported, e.g. `border-radius`, `flex`

## 2019.2

* [`StyleSheetImporterImpl.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.2/Modules/StyleSheetsEditor/StyleSheetImporterImpl.cs)
* [`VisualElementStylesData.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.2/Modules/UIElements/StyleSheets/VisualElementStylesData.cs)
* [`StyleSheetCache.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.2/Modules/UIElements/StyleSheets/StyleSheetCache.cs)
* [`StyleSheetApplicator`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.2/Modules/UIElements/StyleSheets/StyleSheetApplicator.cs)

Removes `unset`. Note that `initial` is handled by `VisualElementStylesData.ApplyStyleValue`. It checks for a value of
`initial` first, and then applies the normal style if not set.

* Introduces `padding` shorthand property


## 2019.3 (beta)

* [`StyleSheetImporterImpl.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.3/Modules/StyleSheetsEditor/StyleSheetImporterImpl.cs)
* [`VisualElementStylesData.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.3/Modules/UIElements/StyleSheets/VisualElementStylesData.cs)
* [`StyleSheetCache.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.3/Modules/UIElements/StyleSheets/StyleSheetCache.cs)
* [`StyleSheetApplicator`](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.3/Modules/UIElements/StyleSheets/StyleSheetApplicator.cs)

The implementation changes again. Now, in `StylePropertyCache`, there is a dictionary of property names to CSS style
syntax, rather than property IDs. E.g. `<length> | <percentage> | auto`. The style sheet asset importer has also changed,
and supports number, pixel and percentage. Number is treated as "float", while pixel and percentage is treated as
"dimension". Properties beginning with `--` are treated as variables.
 
It validates the `var()` function. First argument is the variable name - it must begin with `--`. Second argument is
required and is the fallback value.

* Introduces the `var()` function
* Introduces the `border-*-color` properties
* Introduces the `-unity-overflow-clip-box` property

----

Note that StyleSheetCache.cs sets up initial values, for 2019.1 at least
2019.1 has inline styles. Is this the first version?