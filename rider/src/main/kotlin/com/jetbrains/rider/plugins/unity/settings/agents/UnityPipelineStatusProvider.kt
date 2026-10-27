package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.diagnostic.rethrowControlFlowException
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
import com.intellij.ide.trustedProjects.TrustedProjects
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.application.asContextElement
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.platform.ide.progress.withBackgroundProgress
import com.jetbrains.rider.plugins.unity.UnityBundle
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext
import org.jetbrains.annotations.ApiStatus
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
    private val core = UnityPipelineCore(::runProcess, ::readUnityPipelineManifestFile)

    override suspend fun read(cli: UnityCliPresentation): UnityPipelinePresentation {
        val solution = unityMcpProject(project)?.let { Path.of(it.path) }
        val gate = unityMcpSectionState(cli, solution != null)
        return core.read(gate, cli.location, cli.installedVersion, solution)
    }

    override suspend fun install(modality: ModalityState, source: UnityAgentsSource) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        val unity = UnityCliStatusProvider.getInstance().read().location ?: return
        val solution = unitySolutionDirectory(project) ?: return
        scope.launch(modality.asContextElement()) {
            withBackgroundProgress(project, UnityBundle.message("unity.agents.pipeline.progress.install")) {
                core.install(unity, solution) { logOperationFinished(UnityAgentsOperation.INSTALLING, source, it) }
            }
        }.join()
    }

    override suspend fun upgrade(modality: ModalityState, source: UnityAgentsSource) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        val unity = UnityCliStatusProvider.getInstance().read().location ?: return
        val solution = unitySolutionDirectory(project) ?: return
        scope.launch(modality.asContextElement()) {
            withBackgroundProgress(project, UnityBundle.message("unity.agents.pipeline.progress.update")) {
                core.upgrade(unity, solution) { logOperationFinished(UnityAgentsOperation.UPDATING, source, it) }
            }
        }.join()
    }

    override suspend fun ensureInstalled(agreed: Boolean, source: UnityAgentsSource): UnityPipelinePackageOutcome {
        val solution = unitySolutionDirectory(project) ?: return UnityPipelinePackageOutcome.Ready
        val unity = UnityCliStatusProvider.getInstance().read().location
        return core.ensureInstalled(agreed, unity, solution) { logOperationFinished(UnityAgentsOperation.INSTALLING, source, it) }
    }

    private fun logOperationFinished(operation: UnityAgentsOperation, source: UnityAgentsSource, result: UnityAgentsResult) {
        UnityForAgentsUsagesCollector.logComponentOperationFinished(project, UnityAgentsSetupStep.PIPELINE_PACKAGE, operation, source, result)
    }

    override fun clearFailure() {
        core.clearFailure()
    }

    private suspend fun runProcess(command: GeneralCommandLine, timeoutMs: Int): ProcessOutput? =
        try {
            UnityCliProcessRunner.getInstance().runProcess(command, timeoutMs)
        }
        catch (e: Throwable) {
            rethrowControlFlowException(e)
            logger.warn("Rider could not run ${command.exePath}", e)
            null
        }
}

@ApiStatus.Internal
class UnityPipelineCore(
    private val runProcess: suspend (GeneralCommandLine, Int) -> ProcessOutput?,
    private val readManifest: suspend (Path) -> UnityPipelineManifest?,
    private val now: () -> Long = System::currentTimeMillis,
) {
    private val packageLock = Mutex()
    private val latestLock = Mutex()

    @Volatile
    private var cachedLatest: CachedLatest? = null
    private val runningOperation = AtomicReference<UnityPipelineOperation?>(null)
    private val lastFailure = AtomicReference<FailureRecord?>(null)

    suspend fun read(
        gate: UnityMcpSectionState,
        unity: Path?,
        cliVersion: String?,
        solution: Path?,
    ): UnityPipelinePresentation {
        val latestVersion = if (gate.canAct && unity != null && cliVersion != null) latestVersion(unity, cliVersion) else null
        val operation = runningOperation.get()
        val failure = lastFailure.get()
        val manifestPath = solution?.let { unityManifestPath(it).toString() }
        val manifest = solution?.let { readManifest(it) }
        return UnityPipelinePresentation(
            gate = gate,
            manifestPath = manifestPath,
            manifest = manifest,
            latestVersion = latestVersion,
            operation = operation,
            failure = failure?.failure,
            failureExitCode = failure?.exitCode,
            failureCliError = failure?.cliError,
            failedOperation = failure?.operation,
        )
    }

    private suspend fun latestVersion(unity: Path, cliVersion: String): String? {
        freshLatest(unity, cliVersion)?.let { return it.version }
        return latestLock.withLock {
            val fresh = freshLatest(unity, cliVersion)
            if (fresh != null) {
                fresh.version
            }
            else {
                readLatestVersion(unity, cliVersion)
            }
        }
    }

    private fun freshLatest(unity: Path, cliVersion: String): CachedLatest? {
        val cached = cachedLatest ?: return null
        if (cached.unity != unity || cached.cliVersion != cliVersion) return null
        val ttl = if (cached.version == null) FAILED_LATEST_TTL_MS else LATEST_TTL_MS
        return if (now() - cached.readAt < ttl) cached else null
    }

    private suspend fun readLatestVersion(unity: Path, cliVersion: String): String? {
        val command = unityPipelineListVersionsCommandLine(unity)
        val output = runProcess(command, UnityCliProcessRunner.STORE_READ_TIMEOUT_MS)
        val version = if (output != null && !output.isTimeout && output.exitCode == 0) {
            parseUnityPipelineLatestVersion(output.stdout)
        }
        else {
            null
        }
        cachedLatest = CachedLatest(unity, cliVersion, now(), version)
        return version
    }

    fun forgetLatestVersion() {
        cachedLatest = null
    }

    fun clearFailure() {
        lastFailure.set(null)
    }

    suspend fun install(unity: Path, solution: Path, onResult: (UnityAgentsResult) -> Unit = {}): Boolean {
        if (!runningOperation.compareAndSet(null, UnityPipelineOperation.INSTALLING)) {
            logger.info("The Unity Pipeline package install was asked for while ${runningOperation.get()} runs")
            return false
        }
        try {
            lastFailure.set(null)
            packageLock.withLock {
                if (readManifest(solution)?.present == true) return@withLock
                val output = runProcess(
                    unityPipelineInstallCommandLine(unity, solution),
                    UnityCliProcessRunner.STORE_WRITE_TIMEOUT_MS,
                )
                val failure = unityPipelineInstallOutcome(output, readManifest(solution)?.present)
                if (failure != null) {
                    lastFailure.set(FailureRecord(failure, exitCodeOf(output), UnityPipelineOperation.INSTALLING, cliErrorOf(output)))
                    logger.warn("Rider could not add the Unity Pipeline package to $solution: $failure${processOutputForLog(output)}")
                }
                onResult(if (failure == null) UnityAgentsResult.SUCCESS else UnityAgentsResult.FAILED)
            }
        }
        finally {
            runningOperation.set(null)
        }
        return true
    }

    suspend fun upgrade(unity: Path, solution: Path, onResult: (UnityAgentsResult) -> Unit = {}): Boolean {
        if (!runningOperation.compareAndSet(null, UnityPipelineOperation.UPGRADING)) {
            logger.info("The Unity Pipeline package update was asked for while ${runningOperation.get()} runs")
            return false
        }
        try {
            lastFailure.set(null)
            packageLock.withLock {
                if (readManifest(solution)?.present != true) {
                    lastFailure.set(FailureRecord(UnityPipelineFailure.NOT_CONFIRMED, null, UnityPipelineOperation.UPGRADING))
                    logger.warn("Rider could not upgrade the Unity Pipeline package in $solution: package is absent or unreadable")
                    onResult(UnityAgentsResult.FAILED)
                    return@withLock
                }
                val latest = cachedLatest?.version
                if (latest == null) {
                    lastFailure.set(FailureRecord(UnityPipelineFailure.NOT_CONFIRMED, null, UnityPipelineOperation.UPGRADING))
                    logger.warn("Rider could not upgrade the Unity Pipeline package in $solution: the latest version is unknown")
                    onResult(UnityAgentsResult.FAILED)
                    return@withLock
                }
                val output = runProcess(
                    unityPipelineUpgradeCommandLine(unity, solution, latest),
                    UnityCliProcessRunner.STORE_WRITE_TIMEOUT_MS,
                )
                val failure = unityPipelineUpgradeOutcome(output, readManifest(solution)?.version, latest)
                if (failure != null) {
                    lastFailure.set(FailureRecord(failure, exitCodeOf(output), UnityPipelineOperation.UPGRADING, cliErrorOf(output)))
                    logger.warn("Rider could not upgrade the Unity Pipeline package in $solution: $failure${processOutputForLog(output)}")
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
        unity: Path?,
        solution: Path,
        onInstallResult: (UnityAgentsResult) -> Unit = {},
    ): UnityPipelinePackageOutcome =
        packageLock.withLock {
            val present = readManifest(solution)?.present
            when (unityMcpPackageStep(UnityMcpOperation.CONFIGURING, present, agreed)) {
                UnityMcpPackageStep.NONE -> {
                    clearHeldInstallFailure()
                    UnityPipelinePackageOutcome.Ready
                }
                UnityMcpPackageStep.ABORT -> {
                    if (present == null) {
                        logger.warn("Rider will not configure an agent: ${unityManifestPath(solution)} did not answer")
                        UnityPipelinePackageOutcome.Failed(UnityMcpFailure.PACKAGE_UNREADABLE, null)
                    }
                    else {
                        logger.info("Rider will not configure an agent: the user declined the Unity Pipeline package")
                        UnityPipelinePackageOutcome.Declined
                    }
                }
                UnityMcpPackageStep.INSTALL -> {
                    if (unity == null) {
                        UnityPipelinePackageOutcome.Failed(UnityMcpFailure.PACKAGE_INSTALL, null)
                    }
                    else {
                        val claimed = runningOperation.compareAndSet(null, UnityPipelineOperation.INSTALLING)
                        try {
                            val output = runProcess(
                                unityPipelineInstallCommandLine(unity, solution),
                                UnityCliProcessRunner.STORE_WRITE_TIMEOUT_MS,
                            )
                            val failure = unityPipelineInstallOutcome(output, readManifest(solution)?.present)
                            if (failure == null) {
                                clearHeldInstallFailure()
                                onInstallResult(UnityAgentsResult.SUCCESS)
                                UnityPipelinePackageOutcome.Ready
                            }
                            else {
                                logger.warn("Rider could not add the Unity Pipeline package to $solution: $failure${processOutputForLog(output)}")
                                onInstallResult(UnityAgentsResult.FAILED)
                                UnityPipelinePackageOutcome.Failed(UnityMcpFailure.PACKAGE_INSTALL, exitCodeOf(output))
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
        }

    private fun clearHeldInstallFailure() {
        lastFailure.updateAndGet {
            when (it?.operation) {
                null, UnityPipelineOperation.INSTALLING -> null
                UnityPipelineOperation.UPGRADING -> it
            }
        }
    }

    private fun exitCodeOf(output: ProcessOutput?): Int? =
        output?.takeIf { !it.isTimeout }?.exitCode

    private fun cliErrorOf(output: ProcessOutput?): String? =
        output?.takeIf { !it.isTimeout }?.let { unityCliErrorMessage(it.stdout) }

    private fun processOutputForLog(output: ProcessOutput?): String {
        if (output == null) return ""
        return "\nstdout: ${output.stdout.trim().take(LOGGED_OUTPUT_CAP)}\nstderr: ${output.stderr.trim().take(LOGGED_OUTPUT_CAP)}"
    }
}

private const val LOGGED_OUTPUT_CAP = 4096

private data class FailureRecord(
    val failure: UnityPipelineFailure,
    val exitCode: Int?,
    val operation: UnityPipelineOperation,
    val cliError: String? = null,
)

private data class CachedLatest(
    val unity: Path,
    val cliVersion: String,
    val readAt: Long,
    val version: String?,
)

private val LATEST_TTL_MS: Long = TimeUnit.HOURS.toMillis(6)

private val FAILED_LATEST_TTL_MS: Long = TimeUnit.MINUTES.toMillis(5)

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
