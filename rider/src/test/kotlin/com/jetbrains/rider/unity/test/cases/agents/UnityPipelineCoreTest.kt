package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsResult
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionState
import com.jetbrains.rider.plugins.unity.settings.agents.UnityOldEditor
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineCore
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineManifest
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelinePackageOutcome
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineRegistry
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineRelease
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
 * Pins the cache, the claim and the lock of the Unity Pipeline core. The class reads no file and contacts no registry.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityPipelineCoreTest {
    private val solution: Path = Path.of("/Users/dev/projects/game")

    private var nowMs: Long = 1_000_000_000L
    private var manifest: UnityPipelineManifest? = UnityPipelineManifest(version = null)
    private var latestAnswer: String? = "0.8.0-exp.1"
    private var minimumAnswer: String? = "6000.0"
    private var registryAnswer: UnityPipelineRegistry? = null
    private var editorVersion: String? = "6000.0.23f1"
    private var registryGate: CompletableDeferred<Unit>? = null
    private var manifestFollowsWrite: Boolean = true
    private var writeSucceeds: Boolean = true
    private val fetchedUrls: MutableList<String> = mutableListOf()
    private val writes: MutableList<Pair<Path, String>> = mutableListOf()

    /** A registry with two releases. 0.9.1-exp.1 needs Unity 6000.0. 1.0.0 needs Unity 6000.2, and it is the latest. */
    private val twoVersionRegistry = UnityPipelineRegistry(
        latest = "1.0.0",
        releases = listOf(
            UnityPipelineRelease("0.9.1-exp.1", "6000.0"),
            UnityPipelineRelease("1.0.0", "6000.2"),
        ),
    )

    private val core = UnityPipelineCore(
        fetchRegistry = { url ->
            fetchedUrls.add(url)
            registryGate?.await()
            registryAnswer ?: latestAnswer?.let { UnityPipelineRegistry(it, listOf(UnityPipelineRelease(it, minimumAnswer))) }
        },
        writeDependency = { solution, version ->
            writes.add(solution to version)
            if (writeSucceeds && manifestFollowsWrite) manifest = UnityPipelineManifest(version = version)
            writeSucceeds
        },
        readManifest = { manifest },
        readEditorVersion = { editorVersion },
        now = { nowMs },
    )

    @Test
    fun `the latest version is read once and served from the cache for the same registry`() = runBlocking {
        val presentation1 = core.read(UnityMcpSectionState.READY, solution)
        val presentation2 = core.read(UnityMcpSectionState.READY, solution)
        assertEquals("0.8.0-exp.1", presentation1.latestVersion)
        assertEquals("0.8.0-exp.1", presentation2.latestVersion)
        assertEquals(1, fetchedUrls.size)
    }

    @Test
    fun `the first read asks the registry of Unity`() = runBlocking {
        core.read(UnityMcpSectionState.READY, solution)
        assertEquals(listOf("https://packages.unity.com/com.unity.pipeline"), fetchedUrls)
    }

    @Test
    fun `a manifest with another registry reads the latest version again`() = runBlocking {
        core.read(UnityMcpSectionState.READY, solution)
        assertEquals(1, fetchedUrls.size)
        manifest = UnityPipelineManifest(version = null, registry = "https://mirror.example")
        core.read(UnityMcpSectionState.READY, solution)
        assertEquals(2, fetchedUrls.size)
        assertEquals("https://mirror.example/com.unity.pipeline", fetchedUrls.last())
    }

    @Test
    fun `the cache expires after six hours`() = runBlocking {
        core.read(UnityMcpSectionState.READY, solution)
        assertEquals(1, fetchedUrls.size)
        nowMs += TimeUnit.HOURS.toMillis(6) - 1
        core.read(UnityMcpSectionState.READY, solution)
        assertEquals(1, fetchedUrls.size)
        nowMs += 1
        core.read(UnityMcpSectionState.READY, solution)
        assertEquals(2, fetchedUrls.size)
    }

    @Test
    fun `a failed registry read caches null for five minutes only`() = runBlocking {
        latestAnswer = null
        val presentation1 = core.read(UnityMcpSectionState.READY, solution)
        assertNull(presentation1.latestVersion)
        assertEquals(1, fetchedUrls.size)
        nowMs += TimeUnit.MINUTES.toMillis(5) - 1000
        val presentation2 = core.read(UnityMcpSectionState.READY, solution)
        assertNull(presentation2.latestVersion)
        assertEquals(1, fetchedUrls.size)
        nowMs += 1000
        val presentation3 = core.read(UnityMcpSectionState.READY, solution)
        assertNull(presentation3.latestVersion)
        assertEquals(2, fetchedUrls.size)
    }

    @Test
    fun `forgetting the latest version reads the registry on the next read`() = runBlocking {
        core.read(UnityMcpSectionState.READY, solution)
        assertEquals(1, fetchedUrls.size)
        core.forgetLatestVersion()
        core.read(UnityMcpSectionState.READY, solution)
        assertEquals(2, fetchedUrls.size)
    }

    @Test
    fun `a read with the gate NO_CLI fetches nothing and answers the manifest`() = runBlocking {
        manifest = UnityPipelineManifest(version = "0.8.0-exp.1")
        val presentation = core.read(UnityMcpSectionState.NO_CLI, solution)
        assertEquals(0, fetchedUrls.size)
        assertEquals(unityManifestPath(solution).toString(), presentation.manifestPath)
        assertEquals(manifest, presentation.manifest)
        assertNull(presentation.latestVersion)
        assertEquals(UnityPipelineOffer.INSTALLED, presentation.offer)
    }

    @Test
    fun `a second install call while one runs returns false`() = runBlocking {
        val gate = holdRegistryRead()
        val first = async { core.install(solution) }
        awaitRegistryRead()
        val second = core.install(solution)
        assertFalse(second)
        gate.complete(Unit)
        val firstResult = first.await()
        assertTrue(firstResult)
        assertEquals(1, writes.size)
    }

    @Test
    fun `ensureInstalled waits for install and answers ready without running twice`() = runBlocking {
        val gate = holdRegistryRead()
        val installJob = async { core.install(solution) }
        awaitRegistryRead()
        val ensureJob = async { core.ensureInstalled(agreed = true, solution) }
        yield()
        gate.complete(Unit)
        val installResult = installJob.await()
        val ensureResult = ensureJob.await()
        assertTrue(installResult)
        assertEquals(UnityPipelinePackageOutcome.Ready, ensureResult)
        assertEquals(1, writes.size)
    }

    @Test
    fun `ensureInstalled declines absent package without agreement and writes nothing`() = runBlocking {
        manifest = UnityPipelineManifest(version = null)
        val outcome1 = core.ensureInstalled(agreed = false, solution)
        assertEquals(UnityPipelinePackageOutcome.Declined, outcome1)
        assertEquals(0, writes.size)

        manifest = null
        val outcome2 = core.ensureInstalled(agreed = false, solution)
        assertEquals(UnityPipelinePackageOutcome.Failed(UnityMcpFailure.PACKAGE_UNREADABLE), outcome2)
        assertEquals(0, writes.size)

        manifest = UnityPipelineManifest(version = "0.8.0-exp.1")
        val outcome3 = core.ensureInstalled(agreed = false, solution)
        assertEquals(UnityPipelinePackageOutcome.Ready, outcome3)
        assertEquals(0, writes.size)
    }

    @Test
    fun `a write that fails holds MANIFEST_WRITE`() = runBlocking {
        writeSucceeds = false
        val installed = core.install(solution)
        assertTrue(installed)
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityPipelineFailure.MANIFEST_WRITE, presentation.failure)
        assertEquals(UnityPipelineOperation.INSTALLING, presentation.failedOperation)
        assertEquals(UnityPipelineOffer.FAILED, presentation.offer)
    }

    @Test
    fun `a write that the manifest does not show holds NOT_CONFIRMED`() = runBlocking {
        manifestFollowsWrite = false
        val installed = core.install(solution)
        assertTrue(installed)
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityPipelineFailure.NOT_CONFIRMED, presentation.failure)
        assertEquals(UnityPipelineOffer.FAILED, presentation.offer)
    }

    @Test
    fun `a failed install is held until clearFailure is called`() = runBlocking {
        writeSucceeds = false
        val installed = core.install(solution)
        assertTrue(installed)
        val failed = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityPipelineOffer.FAILED, failed.offer)
        assertEquals(UnityPipelineOperation.INSTALLING, failed.failedOperation)

        core.clearFailure()
        val cleared = core.read(UnityMcpSectionState.READY, solution)
        assertNull(cleared.failure)
        assertNull(cleared.failedOperation)
        assertEquals(UnityPipelineOffer.INSTALL, cleared.offer)
    }

    @Test
    fun `an install by Auto-Configure clears the failure of an earlier button install`() = runBlocking {
        writeSucceeds = false
        val installed = core.install(solution)
        assertTrue(installed)
        val failed = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityPipelineOffer.FAILED, failed.offer)

        writeSucceeds = true
        val outcome = core.ensureInstalled(agreed = true, solution)
        assertEquals(UnityPipelinePackageOutcome.Ready, outcome)
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertNotEquals(UnityPipelineOffer.FAILED, presentation.offer)
    }

    @Test
    fun `an install reads the registry again after a cached failure`() = runBlocking {
        latestAnswer = null
        core.read(UnityMcpSectionState.READY, solution)
        assertEquals(1, fetchedUrls.size)

        latestAnswer = "0.9.1-exp.1"
        val results = mutableListOf<UnityAgentsResult>()
        core.install(solution) { results.add(it) }
        assertEquals(2, fetchedUrls.size)
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
        assertEquals(listOf(solution to "0.9.1-exp.1"), writes)
    }

    @Test
    fun `upgrade of an absent package writes nothing and holds NOT_CONFIRMED`() = runBlocking {
        manifest = UnityPipelineManifest(version = null)
        val upgraded = core.upgrade(solution)
        assertTrue(upgraded)
        assertEquals(0, writes.size)
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityPipelineFailure.NOT_CONFIRMED, presentation.failure)
        assertEquals(UnityPipelineOperation.UPGRADING, presentation.failedOperation)
        assertEquals(UnityPipelineOffer.FAILED, presentation.offer)
    }

    @Test
    fun `upgrade without a known latest version writes nothing and holds REGISTRY_UNREACHABLE`() = runBlocking {
        manifest = UnityPipelineManifest(version = "0.7.0-exp.1")
        latestAnswer = null
        val results = mutableListOf<UnityAgentsResult>()
        val upgraded = core.upgrade(solution) { results.add(it) }
        assertTrue(upgraded)
        assertEquals(listOf(UnityAgentsResult.FAILED), results)
        assertEquals(0, writes.size)
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityPipelineFailure.REGISTRY_UNREACHABLE, presentation.failure)
        assertEquals(UnityPipelineOperation.UPGRADING, presentation.failedOperation)
    }

    @Test
    fun `an upgrade without the network holds REGISTRY_UNREACHABLE until clearFailure is called`() = runBlocking {
        manifest = UnityPipelineManifest(version = "0.7.0-exp.1")
        latestAnswer = null
        core.upgrade(solution)
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityPipelineOffer.FAILED, presentation.offer)
        assertEquals(UnityPipelineFailure.REGISTRY_UNREACHABLE, presentation.failure)

        core.clearFailure()
        assertNull(core.read(UnityMcpSectionState.READY, solution).failure)
    }

    @Test
    fun `an install that lands reports SUCCESS`() = runBlocking {
        val results = mutableListOf<UnityAgentsResult>()
        core.install(solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
    }

    @Test
    fun `an install whose write fails reports FAILED`() = runBlocking {
        writeSucceeds = false
        val results = mutableListOf<UnityAgentsResult>()
        core.install(solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.FAILED), results)
    }

    @Test
    fun `an install of a present package writes nothing and reports nothing`() = runBlocking {
        manifest = UnityPipelineManifest(version = "0.8.0-exp.1")
        val results = mutableListOf<UnityAgentsResult>()
        core.install(solution) { results.add(it) }
        assertEquals(emptyList<UnityAgentsResult>(), results)
        assertEquals(0, writes.size)
    }

    @Test
    fun `a second install call while one runs reports nothing`() = runBlocking {
        val gate = holdRegistryRead()
        val firstResults = mutableListOf<UnityAgentsResult>()
        val secondResults = mutableListOf<UnityAgentsResult>()
        val first = async { core.install(solution) { firstResults.add(it) } }
        awaitRegistryRead()
        core.install(solution) { secondResults.add(it) }
        gate.complete(Unit)
        first.await()
        assertEquals(listOf(UnityAgentsResult.SUCCESS), firstResults)
        assertEquals(emptyList<UnityAgentsResult>(), secondResults)
    }

    @Test
    fun `an upgrade installs the cached latest version and reports SUCCESS`() = runBlocking {
        manifest = UnityPipelineManifest(version = "0.7.0-exp.1")
        core.read(UnityMcpSectionState.READY, solution)
        val results = mutableListOf<UnityAgentsResult>()
        core.upgrade(solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
        assertEquals(1, fetchedUrls.size)
        assertEquals(listOf(solution to "0.8.0-exp.1"), writes)
    }

    @Test
    fun `an upgrade of an absent package reports FAILED`() = runBlocking {
        manifest = UnityPipelineManifest(version = null)
        val results = mutableListOf<UnityAgentsResult>()
        core.upgrade(solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.FAILED), results)
    }

    @Test
    fun `ensureInstalled reports the install it runs and nothing for a present package`() = runBlocking {
        val results = mutableListOf<UnityAgentsResult>()
        core.ensureInstalled(agreed = true, solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)

        core.ensureInstalled(agreed = true, solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
    }

    @Test
    fun `ensureInstalled reports nothing when the user declines the package`() = runBlocking {
        val results = mutableListOf<UnityAgentsResult>()
        core.ensureInstalled(agreed = false, solution) { results.add(it) }
        assertEquals(emptyList<UnityAgentsResult>(), results)
    }

    @Test
    fun `ensureInstalled with a write that fails gives PACKAGE_INSTALL`() = runBlocking {
        writeSucceeds = false
        val results = mutableListOf<UnityAgentsResult>()
        val outcome = core.ensureInstalled(agreed = true, solution) { results.add(it) }
        assertEquals(UnityPipelinePackageOutcome.Failed(UnityMcpFailure.PACKAGE_INSTALL), outcome)
        assertEquals(listOf(UnityAgentsResult.FAILED), results)
    }

    @Test
    fun `an install for an Editor older than the package writes nothing and holds EDITOR_TOO_OLD`() = runBlocking {
        editorVersion = "2022.3.62f1"
        val installed = core.install(solution)
        assertTrue(installed)
        assertEquals(0, writes.size)
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityPipelineFailure.EDITOR_TOO_OLD, presentation.failure)
        assertEquals(UnityPipelineOperation.INSTALLING, presentation.failedOperation)
        assertEquals(UnityPipelineOffer.EDITOR_TOO_OLD, presentation.offer)
    }

    @Test
    fun `an upgrade for an Editor older than the package writes nothing and holds EDITOR_TOO_OLD`() = runBlocking {
        manifest = UnityPipelineManifest(version = "0.7.0-exp.1")
        editorVersion = "2022.3.62f1"
        val results = mutableListOf<UnityAgentsResult>()
        core.upgrade(solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.FAILED), results)
        assertEquals(0, writes.size)
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityPipelineFailure.EDITOR_TOO_OLD, presentation.failure)
        assertEquals(UnityPipelineOperation.UPGRADING, presentation.failedOperation)
        assertEquals(UnityPipelineOffer.FAILED, presentation.offer)
    }

    @Test
    fun `ensureInstalled for an Editor older than the package writes nothing and gives PACKAGE_INSTALL`() = runBlocking {
        editorVersion = "2022.3.62f1"
        val outcome = core.ensureInstalled(agreed = true, solution)
        assertEquals(UnityPipelinePackageOutcome.Failed(UnityMcpFailure.PACKAGE_INSTALL), outcome)
        assertEquals(0, writes.size)
    }

    @Test
    fun `a release without a minimum Editor installs for an Editor of any version`() = runBlocking {
        editorVersion = "2022.3.62f1"
        minimumAnswer = null
        val results = mutableListOf<UnityAgentsResult>()
        core.install(solution) { results.add(it) }
        assertEquals(listOf(UnityAgentsResult.SUCCESS), results)
        assertEquals(listOf(solution to "0.8.0-exp.1"), writes)
    }

    @Test
    fun `a read with the gate NO_CLI fetches nothing and gives the fallback minimum and the Editor version`() = runBlocking {
        val presentation = core.read(UnityMcpSectionState.NO_CLI, solution)
        assertEquals(0, fetchedUrls.size)
        assertEquals("6000.0", presentation.minimumEditorVersion)
        assertEquals("6000.0.23f1", presentation.editorVersion)
    }

    @Test
    fun `a read with the gate READY gives the minimum of the release`() = runBlocking {
        minimumAnswer = "2023.2"
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals("2023.2", presentation.minimumEditorVersion)
        assertEquals("6000.0.23f1", presentation.editorVersion)
    }

    @Test
    fun `an Editor that supports only the older release installs that release`() = runBlocking {
        registryAnswer = twoVersionRegistry
        editorVersion = "6000.0.40f1"
        assertTrue(core.install(solution))
        assertEquals(listOf(solution to "0.9.1-exp.1"), writes)
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals("0.9.1-exp.1", presentation.latestVersion)
        assertNull(presentation.blockingEditor)
    }

    @Test
    fun `an upgrade offers the older release that the Editor supports, and installs it`() = runBlocking {
        registryAnswer = twoVersionRegistry
        editorVersion = "6000.0.40f1"
        manifest = UnityPipelineManifest(version = "0.8.0-exp.1")
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals("0.9.1-exp.1", presentation.latestVersion)
        assertEquals(UnityPipelineOffer.UPGRADE, presentation.offer)
        assertTrue(core.upgrade(solution))
        assertEquals(listOf(solution to "0.9.1-exp.1"), writes)
    }

    @Test
    fun `an Editor that no release supports blocks the package, and the install holds EDITOR_TOO_OLD`() = runBlocking {
        registryAnswer = twoVersionRegistry
        editorVersion = "2022.3.62f1"
        val blocked = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityOldEditor("6000.0", "2022.3.62f1"), blocked.blockingEditor)
        assertTrue(core.install(solution))
        assertEquals(0, writes.size)
        val presentation = core.read(UnityMcpSectionState.READY, solution)
        assertEquals(UnityPipelineFailure.EDITOR_TOO_OLD, presentation.failure)
    }

    @Test
    fun `an Editor that supports the newest release installs the newest release`() = runBlocking {
        registryAnswer = twoVersionRegistry
        editorVersion = "6000.2.1f1"
        assertTrue(core.install(solution))
        assertEquals(listOf(solution to "1.0.0"), writes)
    }

    /** Sets a registry read that waits until the test completes the returned gate. */
    private fun holdRegistryRead(): CompletableDeferred<Unit> =
        CompletableDeferred<Unit>().also { registryGate = it }

    /** Lets a started install reach its registry read. */
    private suspend fun awaitRegistryRead() {
        while (fetchedUrls.isEmpty()) yield()
    }
}
