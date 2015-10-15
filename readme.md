# Unity for ReSharper

This plugin adds basic support for [Unity](http://unity3d.com/) to ReSharper 9.2.

Current features:

* ReSharper knows about classes that derive from `UnityEngine.MonoBehaviour` and the classes, public fields and methods are no longer marked as unused when Solution Wide Analysis is enabled.
* Alt+Insert on MonoBehaviour classes to generate event handlers (without the proper method parameters for now)

That's it! If you want anything else, [suggest it](https://github.com/JetBrains/resharper-unity/issues)!

## Installing

Use ReSharper's Extension Manager (ReSharper &rarr; Extension Manager), search for "Unity" and install. Restart, and it'll just start working.

Please watch the repo or follow [@citizenmatt](https://twitter.com/citizenmatt) and [@slavikt](https://twitter.com/slavikt) on twitter for updates.

## Roadmap

There is no roadmap as such. I am not a Unity developer, so do not know what the common pain points are. If you'd like to suggest a feature, please [raise an issue](https://github.com/JetBrains/resharper-unity/issues).

Some ideas:
 
 * Convert void method into CoRoutine (and updating usages)
