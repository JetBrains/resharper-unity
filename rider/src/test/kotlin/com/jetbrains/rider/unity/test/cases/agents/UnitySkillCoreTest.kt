package com.jetbrains.rider.unity.test.cases.agents

import com.google.gson.Gson
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsResult
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionState
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillClient
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillCore
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillInstall
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnitySkillTarget
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.async
import kotlinx.coroutines.runBlocking
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnitySkillCoreTest {
    private val unity: Path = Path.of("/Users/dev/.unity/bin/unity")
    private val solution: Path = Path.of("/Users/dev/projects/game")

    private val executedCommands: MutableList<List<String>> = mutableListOf()
    private val installs: MutableList<UnitySkillInstall> = mutableListOf()
    private var processAnswer: suspend (GeneralCommandLine) -> ProcessOutput? = answerWithList { cmd ->
        val params = cmd.parametersList.parameters
        if (params.contains("install")) {
            val client = UnitySkillClient.entries.firstOrNull { it.key in params }?.key ?: "unknown"
            // The absolute path keeps the skill inside the solution on Windows too, where a rooted path gets a drive letter.
            installs.add(UnitySkillInstall(client, local = true, solution.toAbsolutePath().normalize().resolve(".agents/skills/unity-cli")))
        }
        output(0)
    }

    private val core = UnitySkillCore(
        runProcess = { command, _ ->
            executedCommands.add(command.parametersList.parameters)
            processAnswer(command)
        },
        exists = { true },
    )

    private fun answerWithList(onInstall: (GeneralCommandLine) -> ProcessOutput?): suspend (GeneralCommandLine) -> ProcessOutput? = { cmd ->
        val params = cmd.parametersList.parameters
        if (params.contains("list")) {
            output(0, skillListJson(installs))
        }
        else {
            onInstall(cmd)
        }
    }

    private fun output(exitCode: Int, stdout: String = ""): ProcessOutput =
        ProcessOutput(stdout, "", exitCode, false, false)

    private fun skillListJson(items: List<UnitySkillInstall>): String {
        val data = items.map {
            mapOf(
                "skill" to "unity-cli",
                "client" to it.client,
                "local" to it.local,
                "path" to it.path.toString(),
            )
        }
        return Gson().toJson(
            mapOf(
                "success" to true,
                "command" to "skill-list",
                "data" to data,
                "errors" to emptyList<String>(),
                "warnings" to emptyList<String>(),
            ),
        )
    }

    @Test
    fun `install ALL runs codex then claude-code and records no failure when both land`() = runBlocking {
        core.install(unity, solution, UnitySkillTarget.ALL.clients)

        val installCommands = executedCommands.filter { it.contains("install") }
        assertEquals(2, installCommands.size)
        assertTrue(installCommands[0].contains("codex"))
        assertTrue(installCommands[1].contains("claude-code"))

        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, unity = unity, solution = solution)
        assertNull(presentation.failure)
        assertNull(presentation.failureExitCode)
        assertEquals(UnitySkillOffer.INSTALLED, presentation.offer)
    }

    @Test
    fun `a nonzero exit records EXIT_CODE with the code and stops before claude-code`() = runBlocking {
        processAnswer = answerWithList { cmd ->
            val params = cmd.parametersList.parameters
            if (params.contains("codex")) {
                output(2)
            }
            else {
                output(0)
            }
        }

        core.install(unity, solution, UnitySkillTarget.ALL.clients)

        val installCommands = executedCommands.filter { it.contains("install") }
        assertEquals(1, installCommands.size)
        assertTrue(installCommands[0].contains("codex"))

        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, unity = unity, solution = solution)
        assertEquals(UnitySkillFailure.EXIT_CODE, presentation.failure)
        assertEquals(2, presentation.failureExitCode)
        assertEquals(UnitySkillOffer.FAILED, presentation.offer)
    }

    @Test
    fun `exit 0 but no local entry after the run records NOT_CONFIRMED`() = runBlocking {
        processAnswer = answerWithList {
            output(0)
        }

        core.install(unity, solution, UnitySkillTarget.CODEX.clients)

        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, unity = unity, solution = solution)
        assertEquals(UnitySkillFailure.NOT_CONFIRMED, presentation.failure)
        assertEquals(0, presentation.failureExitCode)
        assertEquals(UnitySkillOffer.FAILED, presentation.offer)
    }

    @Test
    fun `a null output records LAUNCH_FAILED`() = runBlocking {
        processAnswer = answerWithList {
            null
        }

        core.install(unity, solution, UnitySkillTarget.CODEX.clients)

        val presentation = core.read(UnityMcpSectionState.READY, packageInstalled = true, unity = unity, solution = solution)
        assertEquals(UnitySkillFailure.LAUNCH_FAILED, presentation.failure)
        assertNull(presentation.failureExitCode)
        assertEquals(UnitySkillOffer.FAILED, presentation.offer)
    }

    @Test
    fun `an install that lands reports SUCCESS once`() = runBlocking {
        // The absolute path keeps the skill inside the solution on Windows too, where a rooted path gets a drive letter.
        val skillPath = solution.toAbsolutePath().normalize().resolve(".agents/skills/unity-cli")
        processAnswer = answerWithList { cmd ->
            val params = cmd.parametersList.parameters
            val client = UnitySkillClient.entries.first { it.key in params }.key
            installs.add(UnitySkillInstall(client, local = true, skillPath))
            output(0)
        }

        val results = mutableListOf<UnityAgentsResult>()
        core.install(unity, solution, UnitySkillTarget.CODEX.clients) { results.add(it) }

        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
    }

    @Test
    fun `an install that stops at a failure reports FAILED once`() = runBlocking {
        processAnswer = answerWithList { output(2) }

        val results = mutableListOf<UnityAgentsResult>()
        core.install(unity, solution, UnitySkillTarget.ALL.clients) { results.add(it) }

        assertEquals(listOf(UnityAgentsResult.FAILED), results)
    }

    @Test
    fun `a second install call while one runs reports nothing`() = runBlocking {
        val started = CompletableDeferred<Unit>()
        val release = CompletableDeferred<Unit>()
        processAnswer = { cmd ->
            if (cmd.parametersList.parameters.contains("install")) {
                started.complete(Unit)
                release.await()
            }
            output(0, skillListJson(installs))
        }

        val firstResults = mutableListOf<UnityAgentsResult>()
        val secondResults = mutableListOf<UnityAgentsResult>()
        val first = async { core.install(unity, solution, UnitySkillTarget.CODEX.clients) { firstResults.add(it) } }
        started.await()
        core.install(unity, solution, UnitySkillTarget.CODEX.clients) { secondResults.add(it) }
        release.complete(Unit)
        first.await()

        assertEquals(1, firstResults.size)
        assertEquals(emptyList<UnityAgentsResult>(), secondResults)
    }

    @Test
    fun `read with gate NO_CLI runs no process`() = runBlocking {
        val presentation = core.read(UnityMcpSectionState.NO_CLI, packageInstalled = true, unity = null, solution = solution)
        assertEquals(0, executedCommands.size)
        assertNull(presentation.installs)
        assertEquals(UnitySkillOffer.GATED, presentation.offer)
    }
}
