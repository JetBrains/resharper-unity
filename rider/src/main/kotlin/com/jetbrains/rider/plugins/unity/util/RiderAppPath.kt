@file:Suppress("DEPRECATION")

package com.jetbrains.rider.plugins.unity.util

import com.intellij.ide.actions.CreateDesktopEntryAction
import com.intellij.openapi.application.PathManager
import com.intellij.openapi.util.SystemInfo
import com.sun.jna.Native
import com.sun.jna.Pointer
import com.sun.jna.WString
import com.sun.jna.platform.win32.WinDef
import com.sun.jna.ptr.IntByReference
import com.sun.jna.win32.StdCallLibrary
import java.io.File
import java.io.IOException

// todo: remove, when https://youtrack.jetbrains.com/issue/IDEA-219946 is done

object RiderAppPath {

    fun getPath(): String? {
        if (SystemInfo.isWindows) {
            val kernel32 = Native.loadLibrary("kernel32", Kernel32::class.java)
            // See https://blogs.msdn.microsoft.com/oldnewthing/20060515-07/?p=31203
            // argv[0] as the program name is only a convention, i.e. there is no guarantee
            // the name is the full path to the executable.
            //
            // See https://msdn.microsoft.com/en-us/library/windows/desktop/ms683197(v=vs.85).aspx
            // To retrieve the full path to the executable, use "GetModuleFileName(NULL, ...)".
            //
            // Note: We use 32,767 as buffer size to avoid limiting ourselves to MAX_PATH (260).
            val buffer = CharArray(32767)
            if (kernel32.GetModuleFileNameW(null, buffer, WinDef.DWORD(buffer.size.toLong())).toInt() > 0) {
                return Native.toString(buffer)
            }
            return null
        } else if (SystemInfo.isMac) {
            return macOsAppDir?.path
        } else if (SystemInfo.isUnix) {
            val launcherScript = CreateDesktopEntryAction.getLauncherScript()
            return launcherScript
        } else {
            throw IOException("Cannot restart application: not supported.")
        }
    }

    private val macOsAppDir: File?
        get() {
            val appDir = File(PathManager.getHomePath()).parentFile
            return if (appDir != null && appDir.name.endsWith(".app") && appDir.isDirectory) appDir else null
        }

    private interface Kernel32 : StdCallLibrary {
        fun GetCommandLineW(): WString
        fun LocalFree(pointer: Pointer): Pointer
        fun GetModuleFileNameW(hModule: WinDef.HMODULE?, lpFilename: CharArray, nSize: WinDef.DWORD): WinDef.DWORD
    }

    private interface Shell32 : StdCallLibrary {
        fun CommandLineToArgvW(command_line: WString, argc: IntByReference): Pointer
    }
}