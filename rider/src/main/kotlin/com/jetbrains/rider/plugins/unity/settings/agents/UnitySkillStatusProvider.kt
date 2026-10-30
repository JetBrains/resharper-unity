package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.ide.trustedProjects.TrustedProjects
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.application.asContextElement
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.platform.eel.fs.EelFileUtils
import com.intellij.platform.ide.progress.withBackgroundProgress
import com.intellij.util.SystemProperties
import com.jetbrains.rider.plugins.unity.UnityBundle
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.jetbrains.annotations.ApiStatus
import java.io.ByteArrayInputStream
import java.io.IOException
import java.nio.file.Files
import java.nio.file.Path
import java.util.concurrent.atomic.AtomicBoolean
import java.util.concurrent.atomic.AtomicReference

private val logger: Logger = Logger.getInstance(UnitySkillStatusService::class.java)

private const val STAGING_PREFIX = "unity-cli-skill"

internal interface UnitySkillStatusProvider {
    suspend fun read(cli: UnityCliPresentation, pipeline: UnityPipelinePresentation): UnitySkillPresentation
    suspend fun install(target: UnitySkillTarget, modality: ModalityState, source: UnityAgentsSource)
    /**
     * Installs the skill again for each client that has it in the project, so that it matches a new Unity CLI.
     *
     * A project without the skill stays as it is.
     */
    fun requestRefresh(source: UnityAgentsSource)
    fun clearFailure()

    companion object {
        fun getInstance(project: Project): UnitySkillStatusProvider = project.service<UnitySkillStatusService>()
    }
}

@Service(Service.Level.PROJECT)
internal class UnitySkillStatusService(
    private val project: Project,
    private val scope: CoroutineScope,
) : UnitySkillStatusProvider {
    private val core = UnitySkillCore(
        userHome = { Path.of(SystemProperties.getUserHome()) },
        isFile = { Files.isRegularFile(it) },
        exists = { Files.exists(it) },
        delete = ::deleteWithEmptyParent,
        downloadArchive = ::downloadUnitySkillArchive,
    )

    override suspend fun read(cli: UnityCliPresentation, pipeline: UnityPipelinePresentation): UnitySkillPresentation {
        val solution = unityMcpProject(project)?.let { Path.of(it.path) }
        val gate = unityMcpSectionState(cli, solution != null)
        val packageInstalled = pipeline.manifest?.present == true
        return core.read(gate, packageInstalled, solution).copy(blockingEditor = pipeline.blockingEditor)
    }

    override suspend fun install(target: UnitySkillTarget, modality: ModalityState, source: UnityAgentsSource) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        // The section gate needs a detected Unity CLI, because the skill tells the agents to run it.
        if (UnityCliStatusProvider.getInstance().read().location == null) return
        val solution = unitySolutionDirectory(project) ?: return
        scope.launch(modality.asContextElement()) {
            withBackgroundProgress(project, UnityBundle.message("unity.agents.skills.progress.install")) {
                core.install(solution, target) { logResult(UnityAgentsOperation.INSTALLING, source, it) }
                refreshSkillFolders(solution, target.clients)
            }
        }.join()
    }

    override fun requestRefresh(source: UnityAgentsSource) {
        scope.launch { refresh(source) }
    }

    private suspend fun refresh(source: UnityAgentsSource) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        val solution = unitySolutionDirectory(project) ?: return
        val clients = core.installedClients(solution)
        if (clients.isEmpty()) return
        withBackgroundProgress(project, UnityBundle.message("unity.agents.skills.progress.update")) {
            core.refresh(solution, clients) { logResult(UnityAgentsOperation.UPDATING, source, it) }
            refreshSkillFolders(solution, clients)
        }
    }

    private suspend fun refreshSkillFolders(solution: Path, clients: List<UnitySkillClient>) {
        val folders = clients.mapNotNull { unitySkillDirectory(it, solution).parent?.parent }.distinct()
        if (folders.isEmpty()) return
        // The lookup of each folder refreshes it on this thread, so it runs off the default dispatcher and outside a read action.
        withContext(Dispatchers.IO) {
            VfsUtil.markDirtyAndRefresh(true, true, true, *folders.toTypedArray())
        }
    }

    private fun logResult(operation: UnityAgentsOperation, source: UnityAgentsSource, result: UnityAgentsResult) {
        UnityForAgentsUsagesCollector.logComponentOperationFinished(project, UnityAgentsSetupStep.CLI_SKILL, operation, source, result)
    }

    override fun clearFailure() {
        core.clearFailure()
    }
}

@ApiStatus.Internal
class UnitySkillCore(
    private val userHome: () -> Path,
    private val isFile: (Path) -> Boolean,
    private val exists: (Path) -> Boolean,
    private val delete: (Path) -> Unit,
    private val downloadArchive: suspend () -> ByteArray?,
) {
    private val installing = AtomicBoolean(false)
    private val lastFailure = AtomicReference<SkillFailureRecord?>(null)

    suspend fun read(
        gate: UnityMcpSectionState,
        packageInstalled: Boolean,
        solution: Path?,
    ): UnitySkillPresentation {
        val installs = if ((gate == UnityMcpSectionState.READY || gate == UnityMcpSectionState.CHECKING) && solution != null) {
            withContext(Dispatchers.IO) { unitySkillInstalls(solution, userHome(), isFile) }
        }
        else {
            null
        }
        val failure = lastFailure.get()
        return UnitySkillPresentation(
            gate = gate,
            packageInstalled = packageInstalled,
            installs = installs,
            installing = installing.get(),
            failure = failure?.failure,
            failedTarget = failure?.target,
        )
    }

    /** The clients that have the unity-cli skill in [solution]. */
    suspend fun installedClients(solution: Path): List<UnitySkillClient> = withContext(Dispatchers.IO) {
        UnitySkillClient.entries.filter { isFile(unitySkillDirectory(it, solution).resolve(UNITY_SKILL_FILE)) }
    }

    suspend fun install(
        solution: Path,
        target: UnitySkillTarget,
        onResult: (UnityAgentsResult) -> Unit = {},
    ) {
        runInstall(solution, target.clients, retryTarget = target, onResult)
    }

    /**
     * Installs the skill again for [clients] after an update of the Unity CLI.
     *
     * A failure leaves the old skill in place, and the old skill still works. So Rider logs the failure and holds none.
     */
    suspend fun refresh(
        solution: Path,
        clients: List<UnitySkillClient>,
        onResult: (UnityAgentsResult) -> Unit = {},
    ) {
        runInstall(solution, clients, retryTarget = null, onResult)
    }

    /**
     * Installs the skill for [clients], and logs a failure.
     *
     * @param retryTarget the target that a failure names for Try Again, or null when a failure is only logged
     */
    private suspend fun runInstall(
        solution: Path,
        clients: List<UnitySkillClient>,
        retryTarget: UnitySkillTarget?,
        onResult: (UnityAgentsResult) -> Unit,
    ) {
        if (!installing.compareAndSet(false, true)) {
            logger.info("The unity-cli skill install was asked for while another install runs")
            return
        }
        try {
            if (retryTarget != null) lastFailure.set(null)
            val failure = installFromArchive(solution, clients)
            if (failure != null) {
                if (retryTarget != null) lastFailure.set(SkillFailureRecord(failure, retryTarget))
                logger.warn("Rider could not install the unity-cli skill in $solution: $failure")
            }
            onResult(if (failure == null) UnityAgentsResult.SUCCESS else UnityAgentsResult.FAILED)
        }
        finally {
            installing.set(false)
        }
    }

    private suspend fun installFromArchive(solution: Path, clients: List<UnitySkillClient>): UnitySkillFailure? {
        val archive = downloadArchive() ?: return UnitySkillFailure.DOWNLOAD
        return withContext(Dispatchers.IO) {
            try {
                val staging = Files.createTempDirectory(STAGING_PREFIX)
                try {
                    copySkill(archive, staging, solution, clients)
                }
                finally {
                    EelFileUtils.deleteRecursively(staging)
                }
            }
            catch (e: IOException) {
                logger.warn("Rider could not write the unity-cli skill into $solution", e)
                UnitySkillFailure.WRITE
            }
        }
    }

    /** Extracts the skill into [staging], and copies it into the folder of each client. */
    private suspend fun copySkill(archive: ByteArray, staging: Path, solution: Path, clients: List<UnitySkillClient>): UnitySkillFailure? {
        val extracted = try {
            extractUnitySkill(ByteArrayInputStream(archive), staging)
        }
        catch (e: IOException) {
            logger.warn("Rider could not read the skills archive", e)
            return UnitySkillFailure.DOWNLOAD
        }
        if (!extracted) return UnitySkillFailure.NOT_CONFIRMED
        for (client in clients) {
            val target = unitySkillDirectory(client, solution)
            replaceUnitySkillFolder(staging, target)
            deleteDisabledSkill(target)
        }
        return null
    }

    private suspend fun deleteDisabledSkill(installPath: Path) {
        withContext(Dispatchers.IO) {
            val skills = installPath.parent ?: return@withContext
            // TODO: control the unity-cli skill through the Air skills API instead of a direct delete
            val disabled = skills.resolveSibling("${skills.fileName}.disabled").resolve(installPath.fileName)
            if (!exists(disabled)) return@withContext
            try {
                delete(disabled)
                logger.info("Rider deleted the disabled unity-cli skill at $disabled")
            }
            catch (e: IOException) {
                logger.warn("Rider could not delete the disabled unity-cli skill at $disabled", e)
            }
        }
    }

    fun clearFailure() {
        lastFailure.set(null)
    }
}

/** A failed install, and the target that Try Again installs for again. */
private data class SkillFailureRecord(val failure: UnitySkillFailure, val target: UnitySkillTarget)
