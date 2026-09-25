# PackageShadersGenerated

Unity fixture for **package `.csproj` generation ON** — the regime RIDER-118787's follow-up report came
from, where *some* shader files in a package highlight and others in the same package do not.

Sibling fixture `../PackageShaders/` covers the **generation OFF** regime (no package project at all).
This one exists because "generation on" does not mean every package shader file is listed.

## What the generator does, and therefore what this fixture reproduces

Unity's `com.unity.ide.rider` emits `<None Include/>` for **`.hlsl`, `.shader` and `.compute` only**, and
only for files **inside an asmdef's directory**. Measured on a real project (`unity-6.6`, 99 generated
projects): 645 package shader files on disk, 594 listed, **51 listed nowhere**, 28 of them real assets —
zero `.cginc`, zero `.hlsli`, zero `.urtshader` anywhere.

## The four cases, one file each

| file | in a `.csproj`? | why | expected |
|---|---|---|---|
| `Assets/Shaders/Local.hlsl` | yes, `Assembly-CSharp` | under `Assets/` | control — works today |
| `…/Runtime/Shaders/Fog.hlsl` | yes, `com.test.generatedshaders.Runtime` | listed extension, inside asmdef scope | control — works today |
| `…/Runtime/Shaders/Fog.urtshader` | **no** | extension the generator never emits | **broken**; owning project unambiguous — `Fog.hlsl` sits beside it in `com.test.generatedshaders.Runtime` |
| `…/Runtime/Shaders/FogHelpers.cginc` | **no** | same | **broken**; same, unambiguous |
| `…/Shaders/Standalone.shader` | **no** | outside every asmdef directory | **broken**; no asmdef above it, so the package-wide fallback picks the owner — `Runtime`, because an asmdef under an `Editor` directory is editor-only and is the last resort |
| `…/Documentation~/Ignored.hlsl` | **no** | Unity does not import `~` directories | must **never** get a project item (`UnityExternalFilesModuleProcessor.IsHiddenAssetFolder`) |
| `…/Samples~/S.asmdef` | yes, `com.test.generatedshaders.Samples` | trap for the package-wide fallback | must **never** own `Standalone.shader`, although its path is the shortest (`UnityFileExtensions.IsUnderHiddenAssetFolder`) |

The `Runtime` / `Editor` split is deliberate: it gives `Standalone.shader` a real choice of owner rather than
"obviously the only project there is", and it lets the test assert that an editor-only assembly is not chosen
for a shader.

## The `.Player` projects, and their order in the `.sln`

Unity generates a `.Player` twin of every assembly, and **both twins list the same `.asmdef`**. That makes
"take the first project claiming this asmdef, then ask whether it is suitable" wrong: `IsSuitable` rejects
Player projects, so the real project is never reached and the file is left broken.

The two `.Player` projects exist to pin that, and they are declared **first** in the `.sln` on purpose -
project item enumeration follows solution order, so this is the adversarial ordering. Verified by mutation:
moving the suitability filter after `FirstOrDefault` in `RiderUnityShaderProjectFileCreator.ResolveOwner`
turns the test red, and with the correct ordering the same fixture is green.

The controls legitimately belong to two projects each, because Unity really does list a file in both twins.
The test therefore asserts ownership among non-Player projects only, and separately asserts that a *created*
file never lands in a Player project at all.

All HLSL bodies are byte-identical to `../PackageShaders/`, so golds are comparable across both fixtures
and a difference in highlighting is never a difference in source.

## A note on `Documentation~`

The repository's root `.gitignore` has a `*~` rule, which matches the `Documentation~` directory. Its file was
therefore added with `git add -f`. Once tracked, the ignore rule no longer applies to it, so ordinary edits
need nothing special - but a *new* file under any `~` directory here would have to be force-added too.

That arm is not asserted by a test. It documents the boundary and is checked by eye in an IDE run: Unity does
not import `~` directories, `UnityExternalFilesModuleProcessor.IsHiddenAssetFolder` never descends into them,
and so nothing under `Documentation~` may ever receive a project item.
