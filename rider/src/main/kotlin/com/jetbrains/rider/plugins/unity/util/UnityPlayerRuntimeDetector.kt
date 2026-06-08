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

    suspend fun detect(exePath: Path): UnityScriptingBackend {
        return when (val outcome = detectInternal(exePath)) {
            is ScanOutcome.Ok -> outcome.backend
            is ScanOutcome.UnknownFromScanner -> {
                thisLogger().info("Probably Unity older than 6000.6. Could not detect scripting backend for $exePath")
                UnityScriptingBackend.Mono
            }
            is ScanOutcome.ScanFailed -> {
                thisLogger().error("Failed to detect scripting backend for $exePath", outcome.cause)
                UnityScriptingBackend.Unknown
            }
        }
    }

    private suspend fun detectInternal(exePath: Path): ScanOutcome {
        val libPath = resolveUnityPlayerLib(exePath) ?: return ScanOutcome.UnknownFromScanner
        if (!libPath.isRegularFile()) return ScanOutcome.UnknownFromScanner

        val time = libPath.getLastModifiedTime()
        val key = libPath.toAbsolutePath().normalize()
        cache[key]?.let { (cachedTime, cachedResult) ->
            if (cachedTime == time) return ScanOutcome.Ok(cachedResult)
        }

        val scanOutcome = try {
            val scanned = project.solution.frontendBackendModel.getScriptingBackend.startSuspending(libPath.toRd())
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
        if (detectWithOldHeuristics(libPath) == UnityScriptingBackend.IL2CPP) {
            cache[key] = time to UnityScriptingBackend.IL2CPP
            return ScanOutcome.Ok(UnityScriptingBackend.IL2CPP)
        }

        return scanOutcome
    }

    private fun detectWithOldHeuristics(libPath: Path): UnityScriptingBackend {
        // old heuristics for IL2CPP, keep it for Unity versions older than 6000.6
        val extension = when (OS.CURRENT) {
            OS.Windows -> "dll"
            OS.macOS -> "dylib"
            OS.Linux -> "so"
            else -> throw IllegalStateException("Unsupported OS")
        }

        if (libPath.parent.resolve("GameAssembly.$extension").exists()) {
            return UnityScriptingBackend.IL2CPP
        }
        return UnityScriptingBackend.Unknown
    }

    private fun resolveUnityPlayerLib(exePath: Path): Path? {
        return when (OS.CURRENT) {
            OS.macOS -> {
                val appDir = findEnclosingAppBundle(exePath) ?: return null
                appDir.resolve("Contents/Frameworks/UnityPlayer.dylib")
            }
            OS.Windows -> exePath.parent?.resolve("UnityPlayer.dll")
            OS.Linux -> exePath.parent?.resolve("UnityPlayer.so")
            else -> null
        }
    }

    private fun findEnclosingAppBundle(exePath: Path): Path? {
        var current: Path? = exePath.parent
        while (current != null) {
            if (current.name.endsWith(".app")) return current
            current = current.parent
        }
        return null
    }
}
