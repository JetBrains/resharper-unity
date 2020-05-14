# Contributing to resharper-unity

## I don't want to read this whole thing I just have a question!

[![Join the chat at https://gitter.im/JetBrains/resharper-unity](https://badges.gitter.im/JetBrains/resharper-unity.svg)](https://gitter.im/JetBrains/resharper-unity?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
> **Note:** Please don't file an issue to ask a question. You'll get faster results by using the resources below.

## What should I know before I get started?

Please sign the CLA before sending the PR: https://www.jetbrains.com/agreements/cla/.

This plugin has same architecture as Rider itself. Out-of-process ReSharper serves as backend, IntelliJ IDEA serves as frontend. Unity Editor Plugin passes data and requests between Rider backend and Unity.
Communication between all three is defined in 2 models:
 - rider/protocol/src/main/kotlin/model/editorPlugin 
 - rider/protocol/src/main/kotlin/model/rider/

## How do I change, compile and run the plugin locally?

1. Check out main branch
2. Run `build.sh` or `build.ps1` (depending on your OS).

   SDK will be downloaded, packages restored, etc. and everything should compile without errors.
3. In Intellij IDEA open "rider" folder

   Give it some time to run gradle scripts
4. (Optional) Edit both backend and UnityEditor plugin via resharper/src/resharper-unity.sln  
5. In the Gradle toolwindow find and run "runIDE" task. 
It starts an experimental instance of Rider with locally compiled plugin.
[![](https://user-images.githubusercontent.com/1482681/40919579-32795f52-680a-11e8-8656-89a5275e8570.png)]()
