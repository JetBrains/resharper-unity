package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.util.SystemInfo
import com.sun.jna.Native
import com.sun.jna.win32.StdCallLibrary

class Utils {
    companion object {
        fun AllowUnitySetForegroundWindow(unityPid: Int): Boolean {
            if (SystemInfo.isWindows)
                return user32!!.AllowSetForegroundWindow(unityPid)
            else
                return true
        }

        @Suppress("FunctionName")
        private interface User32 : StdCallLibrary {
            fun AllowSetForegroundWindow(id: Int): Boolean
        }

        private val user32 = if (SystemInfo.isWindows) Native.load("user32", User32::class.java) else null

    }
}