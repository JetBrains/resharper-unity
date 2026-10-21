package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsResult
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionState
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineCore
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineManifest
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelinePackageOutcome
import com.jetbrains.rider.plugins.unity.settings.agents.unityManifestPath
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.async
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.yield
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNotEquals
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path
import java.util.concurrent.TimeUnit

/**
 * Pins the cache, the claim and the lock of the Unity Pipeline core. The class runs no process and reads no file.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityPipelineCoreTest {
    private val unity: Path = Path.of("/Users/dev/.unity/bin/unity")
    private val solution: Path = Path.of("/Users/dev/projects/game")
    private val cliVersion: String = "1.0.0-beta.12"

    private var nowMs: Long = 1_000_000_000L
    private var manifest: UnityPipelineManifest? = UnityPipelineManifest(version = null)
    private val executedCommands: MutableList<List<String>> = mutableListOf()
    private var processAnswer: suspend (GeneralCommandLine) -> ProcessOutput? = { output(0, LIST_VERSIONS_JSON) }

    private val core = UnityPipelineCore(
        runProcess = { command, _ ->
            executedCommands.add(command.parametersList.parameters)
            val answer = processAnswer(command)
            if (answer != null && !answer.isTimeout && answer.exitCode == 0 &&
                command.parametersList.parameters.contains("install")
            ) {
                manifest = UnityPipelineManifest(version = "0.8.0-exp.1")
            }
            answer
        },
        readManifest = { manifest },
        now = { nowMs },
    )

    @Test
    fun `the latest version is read once and served from the cache for the same CLI`() = runBlocking {
        val presentation1 = core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        val presentation2 = core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertEquals("0.8.0-exp.1", presentation1.latestVersion)
        assertEquals("0.8.0-exp.1", presentation2.latestVersion)
        assertEquals(1, executedCommands.size)
    }

    @Test
    fun `a different CLI version reads the latest version again`() = runBlocking {
        core.read(UnityMcpSectionState.READY, unity, "1.0.0-beta.11", solution)
        assertEquals(1, executedCommands.size)
        core.read(UnityMcpSectionState.READY, unity, "1.0.0-beta.12", solution)
        assertEquals(2, executedCommands.size)
    }

    @Test
    fun `the cache expires after six hours`() = runBlocking {
        core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertEquals(1, executedCommands.size)
        nowMs += TimeUnit.HOURS.toMillis(6) - 1
        core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertEquals(1, executedCommands.size)
        nowMs += 1
        core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertEquals(2, executedCommands.size)
    }

    @Test
    fun `a failed list-versions caches null for five minutes only`() = runBlocking {
        processAnswer = { output(6) }
        val presentation1 = core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertNull(presentation1.latestVersion)
        assertEquals(1, executedCommands.size)
        nowMs += TimeUnit.MINUTES.toMillis(5) - 1000
        val presentation2 = core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertNull(presentation2.latestVersion)
        assertEquals(1, executedCommands.size)
        nowMs += 1000
        val presentation3 = core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertNull(presentation3.latestVersion)
        assertEquals(2, executedCommands.size)
    }

    @Test
    fun `forgetting the latest version runs the process on the next read`() = runBlocking {
        core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertEquals(1, executedCommands.size)
        core.forgetLatestVersion()
        core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertEquals(2, executedCommands.size)
    }

    @Test
    fun `a gated read runs no process and answers the manifest`() = runBlocking {
        manifest = UnityPipelineManifest(version = "0.8.0-exp.1")
        val presentation = core.read(UnityMcpSectionState.NO_CLI, unity = null, cliVersion = null, solution)
        assertEquals(0, executedCommands.size)
        assertEquals(unityManifestPath(solution).toString(), presentation.manifestPath)
        assertEquals(manifest, presentation.manifest)
        assertNull(presentation.latestVersion)
        assertEquals(UnityPipelineOffer.GATED, presentation.offer)
    }

    @Test
    fun `a second install call while one runs returns false`() = runBlocking {
        val started = CompletableDeferred<Unit>()
        val release = CompletableDeferred<Unit>()
        processAnswer = {
            started.complete(Unit)
            release.await()
            output(0)
        }
        val first = async { core.install(unity, solution) }
        started.await()
        val second = core.install(unity, solution)
        assertFalse(second)
        release.complete(Unit)
        val firstResult = first.await()
        assertTrue(firstResult)
        assertEquals(1, executedCommands.size)
    }

    @Test
    fun `ensureInstalled waits for install and answers ready without running twice`() = runBlocking {
        val started = CompletableDeferred<Unit>()
        val release = CompletableDeferred<Unit>()
        processAnswer = {
            started.complete(Unit)
            release.await()
            output(0)
        }
        val installJob = async { core.install(unity, solution) }
        started.await()
        val ensureJob = async { core.ensureInstalled(agreed = true, unity, solution) }
        yield()
        release.complete(Unit)
        val installResult = installJob.await()
        val ensureResult = ensureJob.await()
        assertTrue(installResult)
        assertEquals(UnityPipelinePackageOutcome.Ready, ensureResult)
        assertEquals(1, executedCommands.size)
    }

    @Test
    fun `ensureInstalled declines absent package without agreement and runs nothing`() = runBlocking {
        manifest = UnityPipelineManifest(version = null)
        val outcome1 = core.ensureInstalled(agreed = false, unity, solution)
        assertEquals(UnityPipelinePackageOutcome.Declined, outcome1)
        assertEquals(0, executedCommands.size)

        manifest = null
        val outcome2 = core.ensureInstalled(agreed = false, unity, solution)
        assertEquals(UnityPipelinePackageOutcome.Failed(UnityMcpFailure.PACKAGE_UNREADABLE, null), outcome2)
        assertEquals(0, executedCommands.size)

        manifest = UnityPipelineManifest(version = "0.8.0-exp.1")
        val outcome3 = core.ensureInstalled(agreed = false, unity, solution)
        assertEquals(UnityPipelinePackageOutcome.Ready, outcome3)
        assertEquals(0, executedCommands.size)
    }

    @Test
    fun `a failed install is held until clearFailure is called`() = runBlocking {
        processAnswer = { output(6) }
        val installed = core.install(unity, solution)
        assertTrue(installed)
        val failed = core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertEquals(UnityPipelineOffer.FAILED, failed.offer)
        assertEquals(6, failed.failureExitCode)
        assertEquals(UnityPipelineOperation.INSTALLING, failed.failedOperation)

        core.clearFailure()
        val cleared = core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertNull(cleared.failure)
        assertNull(cleared.failureExitCode)
        assertNull(cleared.failedOperation)
        assertEquals(UnityPipelineOffer.INSTALL, cleared.offer)
    }

    @Test
    fun `an install by Auto-Configure clears the failure of an earlier button install`() = runBlocking {
        processAnswer = { output(6) }
        val installed = core.install(unity, solution)
        assertTrue(installed)
        val failed = core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertEquals(UnityPipelineOffer.FAILED, failed.offer)

        processAnswer = { output(0) }
        val outcome = core.ensureInstalled(agreed = true, unity, solution)
        assertEquals(UnityPipelinePackageOutcome.Ready, outcome)
        val presentation = core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertNotEquals(UnityPipelineOffer.FAILED, presentation.offer)
    }

    @Test
    fun `upgrade of an absent package runs no process and holds not confirmed`() = runBlocking {
        manifest = UnityPipelineManifest(version = null)
        val upgraded = core.upgrade(unity, solution)
        assertTrue(upgraded)
        assertEquals(0, executedCommands.size)
        val presentation = core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        assertEquals(UnityPipelineFailure.NOT_CONFIRMED, presentation.failure)
        assertEquals(UnityPipelineOperation.UPGRADING, presentation.failedOperation)
        assertNull(presentation.failureExitCode)
        assertEquals(UnityPipelineOffer.FAILED, presentation.offer)
    }

    @Test
    fun `an install that lands reports SUCCESS`() = runBlocking {
        val results = mutableListOf<UnityAgentsResult>()
        core.install(unity, solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
    }

    @Test
    fun `an install that exits with an error reports FAILED`() = runBlocking {
        processAnswer = { output(6) }
        val results = mutableListOf<UnityAgentsResult>()
        core.install(unity, solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.FAILED), results)
    }

    @Test
    fun `an install of a present package runs nothing and reports nothing`() = runBlocking {
        manifest = UnityPipelineManifest(version = "0.8.0-exp.1")
        val results = mutableListOf<UnityAgentsResult>()
        core.install(unity, solution) { results.add(it) }
        assertEquals(emptyList<UnityAgentsResult>(), results)
        assertEquals(0, executedCommands.size)
    }

    @Test
    fun `a second install call while one runs reports nothing`() = runBlocking {
        val started = CompletableDeferred<Unit>()
        val release = CompletableDeferred<Unit>()
        processAnswer = {
            started.complete(Unit)
            release.await()
            output(0)
        }
        val firstResults = mutableListOf<UnityAgentsResult>()
        val secondResults = mutableListOf<UnityAgentsResult>()
        val first = async { core.install(unity, solution) { firstResults.add(it) } }
        started.await()
        core.install(unity, solution) { secondResults.add(it) }
        release.complete(Unit)
        first.await()
        assertEquals(listOf(UnityAgentsResult.SUCCESS), firstResults)
        assertEquals(emptyList<UnityAgentsResult>(), secondResults)
    }

    @Test
    fun `an upgrade to the latest version reports SUCCESS`() = runBlocking {
        manifest = UnityPipelineManifest(version = "0.7.0-exp.1")
        core.read(UnityMcpSectionState.READY, unity, cliVersion, solution)
        processAnswer = { command ->
            if (command.parametersList.parameters.contains("upgrade")) manifest = UnityPipelineManifest(version = "0.8.0-exp.1")
            output(0)
        }
        val results = mutableListOf<UnityAgentsResult>()
        core.upgrade(unity, solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
    }

    @Test
    fun `an upgrade of an absent package reports FAILED`() = runBlocking {
        manifest = UnityPipelineManifest(version = null)
        val results = mutableListOf<UnityAgentsResult>()
        core.upgrade(unity, solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.FAILED), results)
    }

    @Test
    fun `ensureInstalled reports the install it runs and nothing for a present package`() = runBlocking {
        val results = mutableListOf<UnityAgentsResult>()
        core.ensureInstalled(agreed = true, unity, solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)

        core.ensureInstalled(agreed = true, unity, solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
    }

    @Test
    fun `ensureInstalled reports nothing when the user declines the package`() = runBlocking {
        val results = mutableListOf<UnityAgentsResult>()
        core.ensureInstalled(agreed = false, unity, solution) { results.add(it) }
        assertEquals(emptyList<UnityAgentsResult>(), results)
    }

    @Test
    fun `ensureInstalled without the Unity CLI fails and reports nothing`() = runBlocking {
        val results = mutableListOf<UnityAgentsResult>()
        val outcome = core.ensureInstalled(agreed = true, unity = null, solution) { results.add(it) }
        assertEquals(UnityPipelinePackageOutcome.Failed(UnityMcpFailure.PACKAGE_INSTALL, null), outcome)
        assertEquals(emptyList<UnityAgentsResult>(), results)
        assertEquals(0, executedCommands.size)
    }

    private fun output(exitCode: Int, stdout: String = ""): ProcessOutput =
        ProcessOutput(stdout, "", exitCode, false, false)

    private companion object {
        val LIST_VERSIONS_JSON = """
            {
              "success": true,
              "command": "pipeline list-versions",
              "data": {
                "packageId": "com.unity.pipeline",
                "latest": "0.8.0-exp.1",
                "versions": ["0.8.0-exp.1", "0.7.0-exp.1", "0.6.0-exp.1"]
              },
              "errors": [],
              "warnings": []
            }
        """.trimIndent()
    }
}
