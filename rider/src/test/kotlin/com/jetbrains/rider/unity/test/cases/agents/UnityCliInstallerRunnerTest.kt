package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.openapi.util.SystemInfo
import com.intellij.testFramework.common.timeoutRunBlocking
import com.intellij.testFramework.junit5.TestApplication
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallerRunner
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import kotlinx.coroutines.async
import kotlinx.coroutines.delay
import kotlinx.coroutines.withTimeoutOrNull
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertNotNull
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.Timeout
import java.util.concurrent.atomic.AtomicLong
import kotlin.time.Duration.Companion.milliseconds
import kotlin.time.Duration.Companion.seconds

/**
 * What Rider does with the one long process of the install and of the update.
 *
 * Every case runs a real short-lived process through [UnityCliInstallerRunner], the same runner the
 * install and the update use. **No case runs the installer script of Unity.** One exits with a code,
 * one cannot start, one writes a line every so often, and one floods. A human verifies the real
 * script separately.
 *
 * The runner needs no project and no dispatcher of the EDT, which is the whole point of the seam.
 * The JUnit [Timeout] is the second, outer bound: it fails a case with a thread dump instead of
 * hanging the suite.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
@TestApplication
class UnityCliInstallerRunnerTest {
    @Test
    @Timeout(20)
    fun `the exit code of the command reaches the caller`() {
        val exitCode = timeoutRunBlocking(timeout = 15.seconds) {
            UnityCliInstallerRunner.getInstance().runInstaller(exitingWithCode()) {}
        }

        assertEquals(EXIT_CODE, exitCode, "the exit code of the installer did not reach the caller")
    }

    @Test
    @Timeout(20)
    fun `a command that cannot start reports no exit code`() {
        val exitCode = timeoutRunBlocking(timeout = 15.seconds) {
            UnityCliInstallerRunner.getInstance()
                .runInstaller(GeneralCommandLine("unity-cli-no-such-program-exists")) {}
        }

        // Null, and never -1. UnityCliFailure.LAUNCH_FAILED is the value that reads this.
        assertNull(exitCode, "a command that never started named an exit code")
    }

    /**
     * The output of the process is the evidence, because the handler stays inside the runner.
     *
     * A killed process writes nothing more. A process that outlived the cancel keeps writing, and
     * the listener of the runner keeps forwarding, so the count would keep rising.
     *
     * The case proves that the process dies. It does not prove that the whole **tree** dies, because
     * the shell below is the one process that writes. The tree is what `killProcess` covers.
     */
    @Test
    @Timeout(40)
    fun `a cancelled wait kills the process, so its output stops`() {
        timeoutRunBlocking(timeout = 35.seconds) {
            val chunks = AtomicLong()
            val running = async {
                UnityCliInstallerRunner.getInstance().runInstaller(heartbeating()) { chunks.incrementAndGet() }
            }

            val started = withTimeoutOrNull(15.seconds) {
                while (chunks.get() == 0L) delay(50.milliseconds)
                true
            }
            assertTrue(started == true, "the heartbeat command wrote nothing, so there was nothing to cancel")

            running.cancel()

            // The kill runs under NonCancellable inside the runner, so it needs room after the
            // cancel. The heartbeat is 200 ms, so three seconds of silence is fifteen missed lines.
            delay(3.seconds)
            val afterKill = chunks.get()
            delay(3.seconds)

            assertEquals(afterKill, chunks.get(), "the process wrote more after the cancel, so it is still alive")
        }
    }

    @Test
    @Timeout(60)
    fun `a flooding command does not forward without bound`() {
        val forwarded = AtomicLong()

        val exitCode = timeoutRunBlocking(timeout = 50.seconds) {
            UnityCliInstallerRunner.getInstance().runInstaller(flooding()) { text ->
                forwarded.addAndGet(text.length.toLong())
            }
        }

        assertNotNull(exitCode, "the flooding command never started")
        // The bound is two-sided on purpose. The lower half proves the flood reached the runner at
        // all, so a command that writes nothing cannot pass this case. The upper half is the cap and
        // one chunk, because one chunk crosses it. A runner with no cap forwards the whole 4 MiB.
        assertTrue(
            forwarded.get() > OUTPUT_FORWARD_CAP_CHARS / 2,
            "the runner forwarded only ${forwarded.get()} characters, so the flood never arrived",
        )
        assertTrue(
            forwarded.get() < 2 * OUTPUT_FORWARD_CAP_CHARS,
            "the runner forwarded ${forwarded.get()} characters, so the cap did not hold",
        )
    }

    private fun exitingWithCode(): GeneralCommandLine =
        if (SystemInfo.isWindows) GeneralCommandLine("cmd.exe", "/c", "exit $EXIT_CODE")
        else GeneralCommandLine("sh", "-c", "exit $EXIT_CODE")

    /**
     * A command that writes one line every 200 ms for a minute, unless something stops it.
     *
     * The loop counts with `seq` and the pipe, and never with a shell variable, so the Kotlin string
     * carries no escaped dollar.
     */
    private fun heartbeating(): GeneralCommandLine =
        if (SystemInfo.isWindows) GeneralCommandLine(
            "powershell.exe",
            "-NoProfile",
            "-NonInteractive",
            "-Command",
            "1..300 | ForEach-Object { Write-Output 'tick'; Start-Sleep -Milliseconds 200 }",
        )
        else GeneralCommandLine("sh", "-c", "seq 300 | while read n; do echo tick; sleep 0.2; done")

    /** A command that writes about 4 MiB as fast as the machine allows, then exits 0. */
    private fun flooding(): GeneralCommandLine =
        if (SystemInfo.isWindows) GeneralCommandLine(
            "powershell.exe",
            "-NoProfile",
            "-NonInteractive",
            "-Command",
            // The quotation marks are single, because the string around them is a Kotlin one.
            "1..1000 | ForEach-Object { Write-Output ('a' * 4096) }",
        )
        else GeneralCommandLine("sh", "-c", "seq 1000 | while read n; do printf '%4096s' ''; done")

    private companion object {
        /** The cap of the runner. It is private there, and this is the number the test pins. */
        const val OUTPUT_FORWARD_CAP_CHARS: Long = 64L * 1024L

        /** No meaning of its own. It is neither 0 nor 1, so no default can pass for it. */
        const val EXIT_CODE = 7
    }
}
