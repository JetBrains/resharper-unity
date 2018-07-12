package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.util.SystemInfo
import com.intellij.util.io.isDirectory
import com.sun.jna.platform.win32.Advapi32Util
import com.sun.jna.platform.win32.WinReg
import java.nio.file.Paths

class UnityInstallationIdentifier {
    companion object {

        // Doesn't look for an actual installation, but for evidence that Unity has been used. Essentially, finds the
        // folder that contains. This isn't strictly accurate, but it's a good indication that Unity has been used, so
        // we can use it to e.g. show the "Open From Unity Editor" action on the welcome screen
        fun hasUnityBeenUsed(): Boolean {
            val home = System.getProperty("user.home")
            return when {
                SystemInfo.isWindows -> {
                    Advapi32Util.registryKeyExists(WinReg.HKEY_CURRENT_USER, "Software\\Unity") ||
                            Advapi32Util.registryKeyExists(WinReg.HKEY_CURRENT_USER, "Software\\Unity Technologies")
                }
                SystemInfo.isMac -> {
                    Paths.get(home, ".local/share/Unity").isDirectory() ||
                            Paths.get(home, "Library/Unity").isDirectory()
                }
                SystemInfo.isLinux -> {
                    val dataHome = System.getenv("XDG_DATA_HOME") ?: Paths.get(home, ".local/share/").toString()
                    Paths.get(dataHome, "unity3d").toFile().isDirectory
                }
                else -> false
            }
        }
    }
}
