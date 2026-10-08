package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.CapturingProcessHandler
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.process.ProcessListener
import com.intellij.execution.process.ProcessOutput
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.util.Key
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.jetbrains.annotations.TestOnly
import java.util.concurrent.atomic.AtomicLong

@Service(Service.Level.APP)
class UnityCliProcessRunner private constructor(
    private val run: suspend (GeneralCommandLine, Int) -> ProcessOutput,
) {
    @Suppress("unused")
    constructor() : this(::runUnityCliProcess)

    suspend fun runProcess(command: GeneralCommandLine, timeoutMs: Int): ProcessOutput = run(command, timeoutMs)

    companion object {
        const val IDENTIFY_TIMEOUT_MS: Int = 10_000
        const val DIAGNOSE_TIMEOUT_MS: Int = 30_000
        const val STORE_READ_TIMEOUT_MS: Int = 15_000
        const val STORE_WRITE_TIMEOUT_MS: Int = 60_000

        fun getInstance(): UnityCliProcessRunner = service()

        @TestOnly
        fun runnerAnswering(run: suspend (GeneralCommandLine, Int) -> ProcessOutput): UnityCliProcessRunner =
            UnityCliProcessRunner(run)
    }
}

private const val OUTPUT_CAP_CHARS: Long = 64L * 1024L

private suspend fun runUnityCliProcess(command: GeneralCommandLine, timeoutMs: Int): ProcessOutput =
    withContext(Dispatchers.IO) {
        val handler = CapturingProcessHandler(command)
        handler.processInput.close()

        // The handler reads stdout and stderr on two threads, so the count must be atomic.
        val charsRead = AtomicLong()
        handler.addProcessListener(object : ProcessListener {
            override fun onTextAvailable(event: ProcessEvent, outputType: Key<*>) {
                if (charsRead.addAndGet(event.text.length.toLong()) > OUTPUT_CAP_CHARS) handler.destroyProcess()
            }
        })
        handler.runProcess(timeoutMs, true)
    }
