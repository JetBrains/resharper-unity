package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import com.intellij.openapi.util.SystemInfo
import com.intellij.testFramework.common.timeoutRunBlocking
import com.intellij.testFramework.junit5.TestApplication
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliProcessRunner
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.Timeout
import kotlin.system.measureTimeMillis
import kotlin.time.Duration.Companion.seconds

/**
 * What Rider does with the process that detection runs, before it ever trusts the binary.
 *
 * A candidate is untrusted by definition, because detection runs it to decide what it even is. Both
 * cases here run a real short-lived process through [UnityCliProcessRunner], the same runner
 * detection uses, and pin its two controls: a closed stdin, and a capped stdout.
 *
 * Neither case names the real Unity CLI. One runs a program that reads stdin until end of file, and
 * the other runs a program that writes far more than the cap allows. The JUnit [Timeout] is the
 * second, outer bound. It fails the test with a thread dump instead of hanging the whole suite, if a
 * defect ever brings back the hang.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
@TestApplication
class UnityCliProcessRunnerTest {
    @Test
    @Timeout(15)
    fun `stdin is closed, so a candidate that waits on it sees end of file at once`() {
        // `cat` and `more` both read stdin until end of file. An open, silent pipe would leave either
        // one waiting, so the inner timeout below would fire instead of a fast, ordinary exit.
        val commandLine = if (SystemInfo.isWindows) {
            GeneralCommandLine("cmd.exe", "/c", "more")
        } else {
            GeneralCommandLine("cat")
        }

        lateinit var output: ProcessOutput
        val elapsedMs = measureTimeMillis {
            output = timeoutRunBlocking(timeout = 10.seconds) {
                UnityCliProcessRunner.getInstance().runProcess(commandLine, timeoutMs = 8_000)
            }
        }
        assertFalse(output.isTimeout, "a candidate that reads stdin timed out, so Rider left stdin open")

        assertTrue(elapsedMs < 5_000, "the candidate took ${elapsedMs}ms to see end of file on stdin")
    }

    @Test
    @Timeout(25)
    fun `an untrusted candidate cannot flood Rider with output`() {
        // `yes` and the PowerShell loop below both write far more than the 64 KiB cap, as fast as the
        // machine allows. With no cap, either one would run for the whole inner timeout.
        val commandLine = if (SystemInfo.isWindows) {
            GeneralCommandLine(
                "powershell.exe",
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                // The dollar is concatenated, because an escape of it inside a string reads badly.
                "while (" + '$' + "true) { Write-Output ('a' * 4096) }",
            )
        } else {
            GeneralCommandLine("yes")
        }

        lateinit var output: ProcessOutput
        val elapsedMs = measureTimeMillis {
            output = timeoutRunBlocking(timeout = 20.seconds) {
                UnityCliProcessRunner.getInstance().runProcess(commandLine, timeoutMs = 15_000)
            }
        }
        assertFalse(output.isTimeout, "the output cap let the inner timeout fire, instead of stopping the flood")

        // On a machine with no load, the cap stops the flood in well under the 15 s inner timeout.
        assertTrue(elapsedMs < 10_000, "the flood ran for ${elapsedMs}ms, so the output cap did not stop it")
    }
}
