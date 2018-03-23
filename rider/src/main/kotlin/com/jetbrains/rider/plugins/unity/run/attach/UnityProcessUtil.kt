package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.process.ProcessInfo

class UnityProcessUtil {

    companion object {
        fun isUnityEditorProcess(processInfo : ProcessInfo) : Boolean {
            val name = processInfo.executableDisplayName
            return (name.startsWith("Unity", true) ||
                name.contains("Unity.app")) &&
                !name.contains("UnityDebug") &&
                !name.contains("UnityShader") &&
                !name.contains("UnityHelper") &&
                !name.contains("Unity Helper") &&
                !name.contains("Unity Hub") &&
                !name.contains("UnityCrashHandler")
        }
    }

}