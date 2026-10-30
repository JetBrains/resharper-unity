package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.diagnostic.rethrowControlFlowException
import com.intellij.ide.trustedProjects.TrustedProjects
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.application.asContextElement
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.platform.ide.progress.withBackgroundProgress
import com.intellij.util.io.HttpRequests
import com.jetbrains.rider.plugins.unity.UnityBundle
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext
import org.jetbrains.annotations.ApiStatus
import java.io.IOException
import java.nio.file.Files
import java.nio.file.Path
import java.util.concurrent.TimeUnit
import java.util.concurrent.atomic.AtomicReference
import kotlin.io.path.readText

private val logger: Logger = Logger.getInstance(UnityPipelineStatusService::class.java)

internal interface UnityPipelineStatusProvider {
    suspend fun read(cli: UnityCliPresentation): UnityPipelinePresentation
    suspend fun install(modality: ModalityState, source: UnityAgentsSource)
    suspend fun upgrade(modality: ModalityState, source: UnityAgentsSource)
    suspend fun ensureInstalled(agreed: Boolean, source: UnityAgentsSource): UnityPipelinePackageOutcome
    fun clearFailure()

    companion object {
        fun getInstance(project: Project): UnityPipelineStatusProvider = project.service<UnityPipelineStatusService>()
    }
}

@Service(Service.Level.PROJECT)
internal class UnityPipelineStatusService(
    private val project: Project,
    private val scope: CoroutineScope,
) : UnityPipelineStatusProvider {
    private val core = UnityPipelineCore(
        fetchRegistry = ::readUnityRegistry,
        writeDependency = ::writeUnityPipelineDependency,
        readManifest = ::readUnityPipelineManifestFile,
        readEditorVersion = ::readUnityEditorVersionFile,
    )

    override suspend fun read(cli: UnityCliPresentation): UnityPipelinePresentation {
        val solution = unityMcpProject(project)?.let { Path.of(it.path) }
        val gate = unityMcpSectionState(cli, solution != null)
        return core.read(gate, solution)
    }

    override suspend fun install(modality: ModalityState, source: UnityAgentsSource) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        // The section gate needs a detected Unity CLI.
        if (UnityCliStatusProvider.getInstance().read().location == null) return
        val solution = unitySolutionDirectory(project) ?: return
        scope.launch(modality.asContextElement()) {
            withBackgroundProgress(project, UnityBundle.message("unity.agents.pipeline.progress.install")) {
                core.install(solution) { logOperationFinished(UnityAgentsOperation.INSTALLING, source, it) }
            }
        }.join()
    }

    override suspend fun upgrade(modality: ModalityState, source: UnityAgentsSource) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        // The section gate needs a detected Unity CLI.
        if (UnityCliStatusProvider.getInstance().read().location == null) return
        val solution = unitySolutionDirectory(project) ?: return
        scope.launch(modality.asContextElement()) {
            withBackgroundProgress(project, UnityBundle.message("unity.agents.pipeline.progress.update")) {
                core.upgrade(solution) { logOperationFinished(UnityAgentsOperation.UPDATING, source, it) }
            }
        }.join()
    }

    override suspend fun ensureInstalled(agreed: Boolean, source: UnityAgentsSource): UnityPipelinePackageOutcome {
        val solution = unitySolutionDirectory(project) ?: return UnityPipelinePackageOutcome.Ready
        return core.ensureInstalled(agreed, solution) { logOperationFinished(UnityAgentsOperation.INSTALLING, source, it) }
    }

    private fun logOperationFinished(operation: UnityAgentsOperation, source: UnityAgentsSource, result: UnityAgentsResult) {
        UnityForAgentsUsagesCollector.logComponentOperationFinished(
            project,
            UnityAgentsSetupStep.PIPELINE_PACKAGE,
            operation,
            source,
            result,
        )
    }

    override fun clearFailure() {
        core.clearFailure()
    }
}

@ApiStatus.Internal
class UnityPipelineCore(
    private val fetchRegistry: suspend (packageUrl: String) -> UnityPipelineRegistry?,
    private val writeDependency: suspend (solution: Path, version: String) -> Boolean,
    private val readManifest: suspend (Path) -> UnityPipelineManifest?,
    private val readEditorVersion: suspend (Path) -> String?,
    private val now: () -> Long = System::currentTimeMillis,
) {
    private val packageLock = Mutex()
    private val latestLock = Mutex()

    @Volatile
    private var cachedLatest: CachedLatest? = null
    private val runningOperation = AtomicReference<UnityPipelineOperation?>(null)
    private val lastFailure = AtomicReference<FailureRecord?>(null)

    suspend fun read(gate: UnityMcpSectionState, solution: Path?): UnityPipelinePresentation {
        val manifest = solution?.let { readManifest(it) }
        val registry = if (gate.canAct && manifest != null) latestRegistry(unityPipelineRegistryUrl(manifest.registry)) else null
        val editorVersion = solution?.let { readEditorVersion(it) }
        val release = registry?.let { unityPipelineReleaseFor(it, editorVersion) }
        val operation = runningOperation.get()
        val failure = lastFailure.get()
        return UnityPipelinePresentation(
            gate = gate,
            manifestPath = solution?.let { unityManifestPath(it).toString() },
            manifest = manifest,
            latestVersion = release?.version,
            operation = operation,
            failure = failure?.failure,
            failedOperation = failure?.operation,
            editorVersion = editorVersion,
            minimumEditorVersion = minimumEditorVersion(registry, release),
        )
    }

    /**
     * The minimum Editor that the page compares with the project Editor.
     *
     * It is the minimum of the release that Rider picked. With no such release, it is the lowest minimum of all releases.
     * Before the registry answers, it is the minimum of every published version.
     */
    private fun minimumEditorVersion(registry: UnityPipelineRegistry?, release: UnityPipelineRelease?): String? = when {
        registry == null -> UNITY_PIPELINE_MINIMUM_EDITOR_VERSION
        release != null -> release.minimumEditorVersion
        else -> unityPipelineLowestMinimumEditor(registry)
    }

    /**
     * The registry document at [packageUrl], from the cache while it is fresh.
     *
     * [retryFailure] skips a cached failure, so that a click reads the registry again.
     */
    private suspend fun latestRegistry(packageUrl: String, retryFailure: Boolean = false): UnityPipelineRegistry? {
        freshLatest(packageUrl, retryFailure)?.let { return it.registry }
        return latestLock.withLock {
            val fresh = freshLatest(packageUrl, retryFailure)
            if (fresh != null) {
                fresh.registry
            }
            else {
                readLatestRegistry(packageUrl)
            }
        }
    }

    private fun freshLatest(packageUrl: String, retryFailure: Boolean): CachedLatest? {
        val cached = cachedLatest ?: return null
        if (cached.packageUrl != packageUrl) return null
        if (cached.registry == null && retryFailure) return null
        val ttl = if (cached.registry == null) FAILED_LATEST_TTL_MS else LATEST_TTL_MS
        return if (now() - cached.readAt < ttl) cached else null
    }

    private suspend fun readLatestRegistry(packageUrl: String): UnityPipelineRegistry? {
        val registry = fetchRegistry(packageUrl)
        cachedLatest = CachedLatest(packageUrl, now(), registry)
        return registry
    }

    fun forgetLatestVersion() {
        cachedLatest = null
    }

    fun clearFailure() {
        lastFailure.set(null)
    }

    suspend fun install(solution: Path, onResult: (UnityAgentsResult) -> Unit = {}): Boolean {
        if (!runningOperation.compareAndSet(null, UnityPipelineOperation.INSTALLING)) {
            logger.info("The Unity Pipeline package install was asked for while ${runningOperation.get()} runs")
            return false
        }
        try {
            lastFailure.set(null)
            packageLock.withLock {
                val manifest = readManifest(solution)
                if (manifest?.present == true) return@withLock
                val failure = writeLatest(solution, manifest)
                if (failure != null) {
                    lastFailure.set(FailureRecord(failure, UnityPipelineOperation.INSTALLING))
                    logger.warn("Rider could not add the Unity Pipeline package to $solution: $failure")
                }
                onResult(if (failure == null) UnityAgentsResult.SUCCESS else UnityAgentsResult.FAILED)
            }
        }
        finally {
            runningOperation.set(null)
        }
        return true
    }

    suspend fun upgrade(solution: Path, onResult: (UnityAgentsResult) -> Unit = {}): Boolean {
        if (!runningOperation.compareAndSet(null, UnityPipelineOperation.UPGRADING)) {
            logger.info("The Unity Pipeline package update was asked for while ${runningOperation.get()} runs")
            return false
        }
        try {
            lastFailure.set(null)
            packageLock.withLock {
                val manifest = readManifest(solution)
                if (manifest?.present != true) {
                    lastFailure.set(FailureRecord(UnityPipelineFailure.NOT_CONFIRMED, UnityPipelineOperation.UPGRADING))
                    logger.warn("Rider could not upgrade the Unity Pipeline package in $solution: package is absent or unreadable")
                    onResult(UnityAgentsResult.FAILED)
                    return@withLock
                }
                val failure = writeLatest(solution, manifest)
                if (failure != null) {
                    lastFailure.set(FailureRecord(failure, UnityPipelineOperation.UPGRADING))
                    logger.warn("Rider could not upgrade the Unity Pipeline package in $solution: $failure")
                }
                onResult(if (failure == null) UnityAgentsResult.SUCCESS else UnityAgentsResult.FAILED)
            }
        }
        finally {
            runningOperation.set(null)
        }
        return true
    }

    suspend fun ensureInstalled(
        agreed: Boolean,
        solution: Path,
        onInstallResult: (UnityAgentsResult) -> Unit = {},
    ): UnityPipelinePackageOutcome =
        packageLock.withLock {
            val manifest = readManifest(solution)
            val present = manifest?.present
            when (unityMcpPackageStep(UnityMcpOperation.CONFIGURING, present, agreed)) {
                UnityMcpPackageStep.NONE -> {
                    clearHeldInstallFailure()
                    UnityPipelinePackageOutcome.Ready
                }
                UnityMcpPackageStep.ABORT -> {
                    if (present == null) {
                        logger.warn("Rider will not configure an agent: ${unityManifestPath(solution)} did not answer")
                        UnityPipelinePackageOutcome.Failed(UnityMcpFailure.PACKAGE_UNREADABLE)
                    }
                    else {
                        logger.info("Rider will not configure an agent: the user declined the Unity Pipeline package")
                        UnityPipelinePackageOutcome.Declined
                    }
                }
                UnityMcpPackageStep.INSTALL -> {
                    val claimed = runningOperation.compareAndSet(null, UnityPipelineOperation.INSTALLING)
                    try {
                        val failure = writeLatest(solution, manifest)
                        if (failure == null) {
                            clearHeldInstallFailure()
                            onInstallResult(UnityAgentsResult.SUCCESS)
                            UnityPipelinePackageOutcome.Ready
                        }
                        else {
                            logger.warn("Rider could not add the Unity Pipeline package to $solution for an agent: $failure")
                            onInstallResult(UnityAgentsResult.FAILED)
                            UnityPipelinePackageOutcome.Failed(UnityMcpFailure.PACKAGE_INSTALL)
                        }
                    }
                    finally {
                        if (claimed) {
                            runningOperation.set(null)
                        }
                    }
                }
            }
        }

    /**
     * Sets the package to the newest version that the project Editor supports, and reads the manifest again to confirm it.
     *
     * Rider writes nothing when no published version supports the project Editor.
     */
    private suspend fun writeLatest(solution: Path, manifest: UnityPipelineManifest?): UnityPipelineFailure? {
        val registry = latestRegistry(unityPipelineRegistryUrl(manifest?.registry), retryFailure = true)
                       ?: return UnityPipelineFailure.REGISTRY_UNREACHABLE
        val editorVersion = readEditorVersion(solution)
        val release = unityPipelineReleaseFor(registry, editorVersion)
        if (release == null) {
            logger.warn("No version of the Unity Pipeline package up to ${registry.latest} supports Unity $editorVersion in $solution")
            return UnityPipelineFailure.EDITOR_TOO_OLD
        }
        if (!writeDependency(solution, release.version)) return UnityPipelineFailure.MANIFEST_WRITE
        return if (readManifest(solution)?.version == release.version) null else UnityPipelineFailure.NOT_CONFIRMED
    }

    private fun clearHeldInstallFailure() {
        lastFailure.updateAndGet {
            when (it?.operation) {
                null, UnityPipelineOperation.INSTALLING -> null
                UnityPipelineOperation.UPGRADING -> it
            }
        }
    }
}

private data class FailureRecord(
    val failure: UnityPipelineFailure,
    val operation: UnityPipelineOperation,
)

private data class CachedLatest(
    val packageUrl: String,
    val readAt: Long,
    val registry: UnityPipelineRegistry?,
)

private val LATEST_TTL_MS: Long = TimeUnit.HOURS.toMillis(6)

private val FAILED_LATEST_TTL_MS: Long = TimeUnit.MINUTES.toMillis(5)

private const val REGISTRY_BODY_CAP_BYTES = 1024 * 1024

/** Reads the registry document at [packageUrl], and the minimum Unity Editor of each published version up to the latest. */
internal suspend fun readUnityRegistry(packageUrl: String): UnityPipelineRegistry? =
    withContext(Dispatchers.IO) {
        try {
            val bytes = HttpRequests.request(packageUrl).productNameAsUserAgent().connect { request ->
                request.inputStream.readNBytes(REGISTRY_BODY_CAP_BYTES + 1)
            }
            if (bytes.size > REGISTRY_BODY_CAP_BYTES) {
                logger.warn("The registry document at $packageUrl is larger than $REGISTRY_BODY_CAP_BYTES bytes")
                null
            }
            else {
                val registry = parseUnityRegistry(String(bytes, Charsets.UTF_8))
                if (registry == null) logger.warn("The registry document at $packageUrl names no latest version that parses")
                registry
            }
        }
        catch (e: IOException) {
            logger.warn("Rider could not read the registry document at $packageUrl", e)
            null
        }
    }

/**
 * Sets the Unity Pipeline package to [version] in `Packages/manifest.json` of [solution].
 *
 * The Unity Editor resolves the package the next time it refreshes.
 */
internal suspend fun writeUnityPipelineDependency(solution: Path, version: String): Boolean {
    val file = unityManifestPath(solution)
    return try {
        writeStoreInIde(file) {
            val content = file.readText()
            val updated = unityManifestWithDependency(content, UNITY_PIPELINE_PACKAGE_ID, version)
            if (updated == null) {
                logger.warn("Rider will not write $file, because it is not a JSON object")
                false
            }
            else {
                if (updated != content) replaceStoreFile(file, updated)
                true
            }
        }
    }
    catch (e: Throwable) {
        rethrowControlFlowException(e)
        logger.warn("Rider could not write $file", e)
        false
    }
}

internal suspend fun readUnityPipelineManifestFile(solution: Path): UnityPipelineManifest? =
    withContext(Dispatchers.IO) {
        val file = unityManifestPath(solution)
        val reading = try {
            if (!Files.isRegularFile(file)) {
                ManifestReading(exists = false)
            }
            else {
                ManifestReading(exists = true, content = file.readText())
            }
        }
        catch (e: Throwable) {
            rethrowControlFlowException(e)
            logger.debug("Rider could not read $file", e)
            ManifestReading(exists = true)
        }
        readUnityPipelineManifest(reading.content, reading.exists)
    }

private data class ManifestReading(val exists: Boolean, val content: String? = null)

/** Reads the Unity Editor version of [solution], or gives null when the file is absent or unreadable. */
internal suspend fun readUnityEditorVersionFile(solution: Path): String? =
    withContext(Dispatchers.IO) {
        val file = unityProjectVersionPath(solution)
        try {
            if (Files.isRegularFile(file)) parseUnityEditorVersion(file.readText()) else null
        }
        catch (e: IOException) {
            logger.debug("Rider could not read $file", e)
            null
        }
    }
