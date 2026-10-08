package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import com.intellij.openapi.util.SystemInfo
import com.intellij.testFramework.common.timeoutRunBlocking
import com.intellij.testFramework.junit5.TestApplication
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_MCP_CODEX_KEY
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliProcessRunner
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry
import com.jetbrains.rider.plugins.unity.settings.agents.configPathOf
import com.jetbrains.rider.plugins.unity.settings.agents.parseUnityMcpClientList
import com.jetbrains.rider.plugins.unity.settings.agents.readCodexEntry
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpConfigureLocalCommandLine
import com.jetbrains.rider.plugins.unity.settings.agents.unityMcpLocalClientListCommandLine
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.fail
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.Timeout
import org.junit.jupiter.api.io.TempDir
import java.nio.file.Files
import java.nio.file.Path
import java.nio.file.attribute.PosixFilePermissions
import kotlin.io.path.readText
import kotlin.time.Duration.Companion.seconds

/**
 * The reads and the writes of the Unity MCP section, with a real process instead of a stand-in for
 * one.
 *
 * [UnityMcpRowPresentationTest] answers for every parse rule, and [UnityMcpWriteTest] answers for
 * every command line. So nothing there proves that the command lines Rider builds run at all.
 * These cases keep the real
 * [UnityCliProcessRunner][com.jetbrains.rider.plugins.unity.settings.agents.UnityCliProcessRunner]
 * and give it stand-in binaries that this test writes.
 *
 * The Codex configure is the only write a tool still does, so these cases stand in for the Unity
 * CLI. `UnityMcpJsonStoreTest` and `UnityMcpCodexStoreTest` answer for every write that Rider does
 * itself.
 *
 * No case runs a real Unity CLI. None reaches the network or a store of the user. Each stand-in
 * records its arguments and prints a payload file.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
@TestApplication
class UnityMcpConfigureStandInTest {
    @TempDir
    lateinit var directory: Path

    @Test
    @Timeout(60)
    fun `the project-local client list of a stand-in Unity CLI reaches the parser`() {
        val unity = writeStandIn("unity", CLIENT_LIST, exitCode = 0)

        val output = run(unityMcpLocalClientListCommandLine(unity, directory))

        assertEquals(0, output.exitCode, "the stand-in Unity CLI did not run")
        val list = parseUnityMcpClientList(output.stdout) ?: fail("the output of the process did not parse")
        assertEquals("/home/tester/Platformer/.codex/config.toml", list.configPathOf(UNITY_MCP_CODEX_KEY))
        assertEquals(UnityMcpEntry.PRESENT, readCodexEntry(list))
    }

    @Test
    @Timeout(60)
    fun `Rider asks the Unity CLI for the project-local list with the documented arguments`() {
        val unity = writeStandIn("unity", CLIENT_LIST, exitCode = 0)

        run(unityMcpLocalClientListCommandLine(unity, directory))

        assertEquals("mcp configure --list --local --json", recordedArguments("unity"))
    }

    @Test
    @Timeout(60)
    fun `Rider asks the Unity CLI to write the project store with the documented arguments`() {
        val unity = writeStandIn("unity", "", exitCode = 0)

        run(unityMcpConfigureLocalCommandLine(unity, directory))

        assertEquals("mcp configure codex --local --yes", recordedArguments("unity"))
    }

    @Test
    @Timeout(60)
    fun `a stand-in Unity CLI that stops leaves the row without an answer`() {
        val unity = writeStandIn("unity", "", exitCode = EXIT_CODE)

        val output = run(unityMcpLocalClientListCommandLine(unity, directory))

        assertEquals(EXIT_CODE, output.exitCode, "the exit code of the process did not reach the caller")
        assertEquals(UnityMcpEntry.UNREADABLE, readCodexEntry(parseUnityMcpClientList(output.stdout)))
    }

    private fun run(command: GeneralCommandLine): ProcessOutput =
        timeoutRunBlocking(timeout = 45.seconds) {
            UnityCliProcessRunner.getInstance().runProcess(command, UnityCliProcessRunner.STORE_READ_TIMEOUT_MS)
        }

    /**
     * Writes a stand-in binary that records its arguments and prints [payload].
     *
     * The paths go into the body, because the caller builds the command line and adds no argument
     * of its own.
     */
    private fun writeStandIn(
        name: String,
        payload: String,
        exitCode: Int,
    ): Path {
        val payloadFile = directory.resolve("$name-payload.txt")
        Files.writeString(payloadFile, payload)
        val argumentsFile = argumentsFile(name)

        val body =
            if (SystemInfo.isWindows) buildString {
                appendLine("@echo off")
                appendLine("""echo %*>"$argumentsFile"""")
                appendLine("""type "$payloadFile"""")
                append("exit /b $exitCode")
            }
            else buildString {
                appendLine("#!/bin/sh")
                appendLine("""printf '%s' "$*" > '$argumentsFile'""")
                appendLine("""cat '$payloadFile'""")
                append("exit $exitCode")
            }

        return writeExecutable(name, body)
    }

    private fun writeExecutable(name: String, body: String): Path {
        val extension = if (SystemInfo.isWindows) ".cmd" else ".sh"
        val binary = directory.resolve(name + extension)
        Files.writeString(binary, body + System.lineSeparator())
        // A Windows `.cmd` needs no bit, and the POSIX permission API does not exist there.
        if (!SystemInfo.isWindows) Files.setPosixFilePermissions(binary, PosixFilePermissions.fromString("rwx------"))
        return binary
    }

    private fun recordedArguments(name: String): String = argumentsFile(name).readText().trim()

    private fun argumentsFile(name: String): Path = directory.resolve("$name-arguments.txt")

    private companion object {
        /** No meaning of its own. It is neither 0 nor 1, so no default can pass for it. */
        const val EXIT_CODE = 7

        const val CLIENT_LIST = """
            {
              "success": true,
              "command": "mcp-clients",
              "data": [
                {"key": "codex", "displayName": "OpenAI Codex CLI",
                 "configPath": "/home/tester/Platformer/.codex/config.toml", "status": "configured"}
              ],
              "errors": [],
              "warnings": []
            }
        """
    }
}
