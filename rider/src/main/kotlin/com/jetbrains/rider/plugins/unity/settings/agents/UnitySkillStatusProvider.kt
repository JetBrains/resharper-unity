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
import kotlinx.coroutines.withContext
import org.jetbrains.annotations.ApiStatus
import java.nio.file.Files
import java.nio.file.Path
import java.util.concurrent.atomic.AtomicBoolean
import java.util.concurrent.atomic.AtomicReference

private val logger: Logger = Logger.getInstance(UnitySkillStatusService::class.java)

internal interface UnitySkillStatusProvider {
    suspend fun read(cli: UnityCliPresentation, pipeline: UnityPipelinePresentation): UnitySkillPresentation
    suspend fun install(target: UnitySkillTarget, modality: ModalityState)
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
    private val core = UnitySkillCore(::runProcess, Files::exists)

    override suspend fun read(cli: UnityCliPresentation, pipeline: UnityPipelinePresentation): UnitySkillPresentation {
        val solution = unityMcpProject(project)?.let { Path.of(it.path) }
        val gate = unityMcpSectionState(cli, solution != null)
        val packageInstalled = pipeline.manifest?.present == true
        return core.read(gate, packageInstalled, cli.location, solution)
    }

    override suspend fun install(target: UnitySkillTarget, modality: ModalityState) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        val unity = UnityCliStatusProvider.getInstance().read().location ?: return
        val solution = unitySolutionDirectory(project) ?: return
        scope.launch(modality.asContextElement()) {
            withBackgroundProgress(project, UnityBundle.message("unity.agents.skills.progress.install")) {
                core.install(unity, solution, target.clients)
            }
        }.join()
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
class UnitySkillCore(
    private val runProcess: suspend (GeneralCommandLine, Int) -> ProcessOutput?,
    private val exists: (Path) -> Boolean,
) {
    private val installing = AtomicBoolean(false)
    private val lastFailure = AtomicReference<SkillFailureRecord?>(null)

    suspend fun read(
        gate: UnityMcpSectionState,
        packageInstalled: Boolean,
        unity: Path?,
        solution: Path?,
    ): UnitySkillPresentation {
        val installs = if ((gate == UnityMcpSectionState.READY || gate == UnityMcpSectionState.CHECKING) && unity != null && solution != null) {
            readInstalls(unity, solution)
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
            failureExitCode = failure?.exitCode,
        )
    }

    private suspend fun readInstalls(unity: Path, solution: Path): List<UnitySkillInstall>? {
        val command = unitySkillListCommandLine(unity)
        val output = runProcess(command, UnityCliProcessRunner.STORE_READ_TIMEOUT_MS)
        if (output == null || output.isTimeout || output.exitCode != 0) return null
        val parsed = parseUnitySkillList(output.stdout) ?: return null
        return withContext(Dispatchers.IO) {
            unitySkillInstallsOf(parsed, solution, exists)
        }
    }

    suspend fun install(unity: Path, solution: Path, clients: List<UnitySkillClient>) {
        if (!installing.compareAndSet(false, true)) {
            logger.info("The unity-cli skill install was asked for while another install runs")
            return
        }
        try {
            lastFailure.set(null)
            for (client in clients) {
                val command = unitySkillInstallCommandLine(unity, client, solution)
                val output = runProcess(command, UnityCliProcessRunner.STORE_WRITE_TIMEOUT_MS)
                val installs = readInstalls(unity, solution)
                val landed = installs?.any { it.client == client.key && it.local } == true
                val failure = unitySkillInstallOutcome(output, landed)
                if (failure != null) {
                    lastFailure.set(SkillFailureRecord(failure, exitCodeOf(output)))
                    logger.warn("Rider could not install the unity-cli skill for ${client.key} in $solution: $failure${processOutputForLog(output)}")
                    break
                }
            }
        }
        finally {
            installing.set(false)
        }
    }

    fun clearFailure() {
        lastFailure.set(null)
    }

    private fun exitCodeOf(output: ProcessOutput?): Int? =
        output?.takeIf { !it.isTimeout }?.exitCode

    private fun processOutputForLog(output: ProcessOutput?): String {
        if (output == null) return ""
        return "\nstdout: ${output.stdout.trim().take(LOGGED_OUTPUT_CAP)}\nstderr: ${output.stderr.trim().take(LOGGED_OUTPUT_CAP)}"
    }
}

private const val LOGGED_OUTPUT_CAP = 4096

private data class SkillFailureRecord(
    val failure: UnitySkillFailure,
    val exitCode: Int?,
)
