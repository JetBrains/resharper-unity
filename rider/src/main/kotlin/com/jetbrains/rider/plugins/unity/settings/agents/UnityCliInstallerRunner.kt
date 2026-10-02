package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.execution.ExecutionException
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.KillableProcessHandler
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.process.ProcessHandler
import com.intellij.execution.process.ProcessListener
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.util.Key
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext
import kotlinx.coroutines.withTimeoutOrNull
import java.util.concurrent.atomic.AtomicBoolean
import java.util.concurrent.atomic.AtomicLong
import kotlin.coroutines.resume
import kotlin.time.Duration.Companion.seconds

private val logger = Logger.getInstance(UnityCliInstallerRunner::class.java)

@Service(Service.Level.APP)
class UnityCliInstallerRunner private constructor(
    val cdnBaseUrl: String,
    private val run: suspend (GeneralCommandLine, (String) -> Unit) -> Int?,
) {
    @Suppress("unused")
    constructor() : this(UNITY_CLI_CDN_BASE_URL, ::runUnityCliInstallerProcess)
    suspend fun runInstaller(commandLine: GeneralCommandLine, onOutput: (String) -> Unit): Int? =
        run(commandLine, onOutput)

    companion object {
        fun getInstance(): UnityCliInstallerRunner = service()
    }
}
private const val OUTPUT_FORWARD_CAP_CHARS: Long = 64L * 1024L

private val GRACEFUL_STOP_TIMEOUT = 1.seconds

internal suspend fun runUnityCliInstallerProcess(
    commandLine: GeneralCommandLine,
    onOutput: (String) -> Unit,
): Int? = withContext(Dispatchers.IO) {
    // The default reader options are blocking, so a chunk reaches onOutput as the process writes it.
    val handler = try {
        KillableProcessHandler(commandLine)
    }
    catch (e: ExecutionException) {
        logger.warn("Rider could not start ${commandLine.exePath}", e)
        return@withContext null
    }

    handler.processInput.close()

    val forwarded = AtomicLong()
    val capReported = AtomicBoolean()
    handler.addProcessListener(object : ProcessListener {
        override fun onTextAvailable(event: ProcessEvent, outputType: Key<*>) {
            if (forwarded.addAndGet(event.text.length.toLong()) > OUTPUT_FORWARD_CAP_CHARS) {
                if (capReported.compareAndSet(false, true)) {
                    logger.warn("${commandLine.exePath} passed $OUTPUT_FORWARD_CAP_CHARS characters of output. The rest is dropped")
                }
                return
            }
            onOutput(event.text)
        }
    })

    try {
        handler.startNotify()
        handler.awaitTermination()
        handler.exitCode
    }
    finally {
        // The stop must run even when the coroutine is already cancelled, so it runs under NonCancellable.
        withContext(NonCancellable) { stopProcessTree(handler) }
    }
}

private suspend fun stopProcessTree(handler: KillableProcessHandler) {
    if (handler.isProcessTerminated) return
    handler.destroyProcess()
    if (withTimeoutOrNull(GRACEFUL_STOP_TIMEOUT) { handler.awaitTermination() } != null) return
    if (handler.isProcessTerminated) return
    if (handler.canKillProcess()) {
        logger.warn("The Unity CLI command did not stop within $GRACEFUL_STOP_TIMEOUT. Rider kills the process tree")
        handler.killProcess()
    }
    else {
        logger.warn("The Unity CLI command did not stop within $GRACEFUL_STOP_TIMEOUT, and Rider cannot kill it")
    }
}

private suspend fun ProcessHandler.awaitTermination() {
    suspendCancellableCoroutine { continuation ->
        val completed = AtomicBoolean()
        val listener = object : ProcessListener {
            fun complete() {
                if (!completed.compareAndSet(false, true)) return
                removeProcessListener(this)
                continuation.resume(Unit)
            }

            override fun processTerminated(event: ProcessEvent) {
                complete()
            }
        }
        addProcessListener(listener)
        continuation.invokeOnCancellation {
            if (completed.compareAndSet(false, true)) removeProcessListener(listener)
        }
        if (isProcessTerminated) listener.complete()
    }
}
