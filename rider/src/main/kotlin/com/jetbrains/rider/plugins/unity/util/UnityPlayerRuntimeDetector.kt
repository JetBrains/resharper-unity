@file:OptIn(LowLevelLocalMachineAccess::class)

package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.project.Project
import com.intellij.util.system.LowLevelLocalMachineAccess
import com.intellij.util.system.OS
import com.jetbrains.rider.ijent.extensions.toRd
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityScriptingBackend
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import kotlinx.coroutines.CancellationException
import java.nio.file.Path
import java.nio.file.attribute.FileTime
import java.util.concurrent.ConcurrentHashMap
import kotlin.io.path.exists
import kotlin.io.path.getLastModifiedTime
import kotlin.io.path.isRegularFile
import kotlin.io.path.name

@Service(Service.Level.PROJECT)
class UnityPlayerRuntimeDetector(val project: Project) {

    companion object {
        fun getInstance(project: Project): UnityPlayerRuntimeDetector = project.service()
    }

    private val cache = ConcurrentHashMap<Path, Pair<FileTime, UnityScriptingBackend>>()

    private sealed class ScanOutcome {
        data class Ok(val backend: UnityScriptingBackend) : ScanOutcome()
        object UnknownFromScanner : ScanOutcome()
        data class ScanFailed(val cause: Throwable) : ScanOutcome()
    }

    suspend fun detect(exePath: Path, isEditor: Boolean): UnityScriptingBackend {
        return when (val outcome = detectInternal(exePath, isEditor)) {
            is ScanOutcome.Ok -> outcome.backend
            is ScanOutcome.UnknownFromScanner -> {
                thisLogger().info("Could not detect the scripting backend for $exePath. " +
                                  "The player exports no backend symbol, and it has no GameAssembly library. " +
                                  "The detector reports Mono. Unity 6000.7 and later export the symbol.")
                UnityScriptingBackend.Mono
            }
            is ScanOutcome.ScanFailed -> {
                thisLogger().error("Failed to detect scripting backend for $exePath", outcome.cause)
                UnityScriptingBackend.Unknown
            }
        }
    }

    private suspend fun detectInternal(exePath: Path, isEditor: Boolean): ScanOutcome {
        // in case of editor, the binary itself is supposed to contain the exported symbol,
        // otherwise we look for the unity player library instead
        val playerPath = if (isEditor) exePath else resolveUnityPlayerLib(exePath)
        if (playerPath == null || !playerPath.isRegularFile()) {
            // The IL2CPP guard reads the native library directory, not the player library, so it
            // still answers when the player library is absent.
            if (detectWithOldHeuristics(exePath) == UnityScriptingBackend.IL2CPP)
                return ScanOutcome.Ok(UnityScriptingBackend.IL2CPP)
            return ScanOutcome.UnknownFromScanner
        }

        val time = playerPath.getLastModifiedTime()
        val key = playerPath.toAbsolutePath().normalize()
        cache[key]?.let { (cachedTime, cachedResult) ->
            if (cachedTime == time) return ScanOutcome.Ok(cachedResult)
        }

        val scanOutcome = try {
            val scanned = project.solution.frontendBackendModel.getScriptingBackend.startSuspending(playerPath.toRd())
            if (scanned == UnityScriptingBackend.Unknown) ScanOutcome.UnknownFromScanner
            else ScanOutcome.Ok(scanned)
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            ScanOutcome.ScanFailed(e)
        }

        if (scanOutcome is ScanOutcome.Ok) {
            cache[key] = time to scanOutcome.backend
            return scanOutcome
        }

        // Scanner could not determine the backend (old Unity) or threw — preserve the IL2CPP guard.
        if (detectWithOldHeuristics(exePath) == UnityScriptingBackend.IL2CPP) {
            cache[key] = time to UnityScriptingBackend.IL2CPP
            return ScanOutcome.Ok(UnityScriptingBackend.IL2CPP)
        }

        return scanOutcome
    }

    private fun detectWithOldHeuristics(exePath: Path): UnityScriptingBackend {
        // The fallback finds IL2CPP by the GameAssembly library. It answers for a Unity older than
        // 6000.7, which is the first version that exports the backend symbol.
        val extension = nativeLibraryExtension ?: return UnityScriptingBackend.Unknown
        val libraryDir = resolveNativeLibraryDirectory(exePath) ?: return UnityScriptingBackend.Unknown

        if (libraryDir.resolve("GameAssembly.$extension").exists()) {
            return UnityScriptingBackend.IL2CPP
        }
        return UnityScriptingBackend.Unknown
    }

    private fun resolveUnityPlayerLib(exePath: Path): Path? {
        val extension = nativeLibraryExtension ?: return null
        return resolveNativeLibraryDirectory(exePath)?.resolve("UnityPlayer.$extension")
    }

    /**
     * The directory that holds `UnityPlayer` and `GameAssembly` for the player at [exePath].
     *
     * On macOS a player is an application bundle, so the caller may name either the inner executable
     * or the bundle itself.
     */
    private fun resolveNativeLibraryDirectory(exePath: Path): Path? {
        return when (OS.CURRENT) {
            OS.macOS -> findAppBundle(exePath)?.resolve("Contents/Frameworks")
            OS.Windows, OS.Linux -> exePath.parent
            else -> null
        }
    }

    private val nativeLibraryExtension: String?
        get() = when (OS.CURRENT) {
            OS.Windows -> "dll"
            OS.macOS -> "dylib"
            OS.Linux -> "so"
            else -> null
        }

    private fun findAppBundle(exePath: Path): Path? {
        var current: Path? = exePath
        while (current != null) {
            if (current.name.endsWith(".app")) return current
            current = current.parent
        }
        return null
    }
}
