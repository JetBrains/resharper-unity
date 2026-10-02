package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.execution.ExecutionException
import com.intellij.execution.process.ProcessOutput
import com.intellij.icons.AllIcons
import com.intellij.ide.actions.ShowLogAction
import com.intellij.ide.trustedProjects.TrustedProjects
import com.intellij.openapi.application.EDT
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.application.asContextElement
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.Logger
import com.intellij.notification.NotificationAction
import com.intellij.notification.NotificationGroupManager
import com.intellij.notification.NotificationType
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.registry.Registry
import com.intellij.platform.ide.progress.withBackgroundProgress
import com.intellij.platform.util.progress.reportSequentialProgress
import com.intellij.util.io.HttpRequests
import com.jetbrains.rider.plugins.unity.UnityBundle
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext
import java.io.IOException
import java.nio.file.Files
import java.nio.file.Path
import java.util.concurrent.TimeUnit
import java.util.concurrent.atomic.AtomicLong
import java.util.concurrent.atomic.AtomicReference

interface UnityCliStatusProvider {
    suspend fun read(): UnityCliPresentation
    suspend fun install(project: Project, modality: ModalityState)
    suspend fun update(project: Project, modality: ModalityState)
    fun requestInstall(project: Project, modality: ModalityState)
    fun requestUpdate(project: Project, modality: ModalityState)
    /**
     * Clears the failure with this id. A failure that arrived later stays, because the user did not see it.
     */
    fun clearFailure(failureId: Long)

    /**
     * Drops the cached answer of the update check. The next [read] asks the CLI and the feed again.
     */
    fun forgetUpdateInfo()

    companion object {
        fun getInstance(): UnityCliStatusProvider = service<UnityCliStatusService>()
    }
}

@Service(Service.Level.APP)
internal class UnityCliStatusService(private val scope: CoroutineScope) : UnityCliStatusProvider {
    private val updateInfoLock = Mutex()

    @Volatile
    private var cachedUpdateInfo: CachedUpdateInfo? = null
    private val runningOperation = AtomicReference<UnityCliOperation?>(null)
    private val lastFailure = AtomicReference<FailureRecord?>(null)
    private val failureIds = AtomicLong()

    override suspend fun read(): UnityCliPresentation = withHeldState(detect())

    override suspend fun install(project: Project, modality: ModalityState) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        if (!runningOperation.compareAndSet(null, UnityCliOperation.INSTALLING)) {
            logger.info("The Unity CLI install was asked for while ${runningOperation.get()} runs")
            return
        }
        scope.launch { runInstall(project, modality) }.join()
    }

    override suspend fun update(project: Project, modality: ModalityState) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        if (!runningOperation.compareAndSet(null, UnityCliOperation.UPDATING)) {
            logger.info("The Unity CLI update was asked for while ${runningOperation.get()} runs")
            return
        }
        scope.launch { runUpdate(project, modality) }.join()
    }

    override fun requestInstall(project: Project, modality: ModalityState) {
        scope.launch { install(project, modality) }
    }

    override fun requestUpdate(project: Project, modality: ModalityState) {
        scope.launch { update(project, modality) }
    }

    override fun forgetUpdateInfo() {
        cachedUpdateInfo = null
    }

    override fun clearFailure(failureId: Long) {
        val current = lastFailure.get() ?: return
        if (current.id != failureId) return
        // The compare keeps a failure that another thread reported after the read above.
        lastFailure.compareAndSet(current, null)
    }

    private suspend fun detect(): UnityCliPresentation {
        val environment = UnityCliEnvironment.getInstance()
        val lookup = findLocalCopy(environment)
        val copy = lookup.copy
            ?: return UnityCliPresentation(detected = true, foreignBinaryOnPath = lookup.foreignBinaryOnPath)

        val update = readUpdateInfo(copy)
        return UnityCliPresentation(
            detected = true,
            installedVersion = copy.version,
            location = copy.path,
            installMethod = installMethodOf(environment, copy, update.info),
            latestVersion = update.latestVersion,
            updateCommand = update.info?.updateCommand,
        )
    }

    private fun withHeldState(detected: UnityCliPresentation): UnityCliPresentation {
        val failure = lastFailure.get()
        return detected.copy(
            operation = runningOperation.get(),
            failure = failure?.failure,
            failureExitCode = failure?.exitCode,
            failedOperation = failure?.operation,
            failureId = failure?.id,
        )
    }

    private suspend fun runInstall(project: Project, modality: ModalityState) {
        try {
            val script = UnityCliInstallerScript.forCurrentOs()
            if (script == null) {
                logger.warn("Unity publishes no installer script for this operating system")
                report(project, modality, UnityCliFailure.DOWNLOAD, null, UnityCliOperation.INSTALLING)
                return
            }

            lastFailure.set(null)
            val runner = UnityCliInstallerRunner.getInstance()
            val result = withBackgroundProgress(project, UnityBundle.message("unity.agents.cli.progress.install")) {
                reportSequentialProgress { reporter ->
                    runUnityCliInstall(
                        script = script,
                        cdnBaseUrl = runner.cdnBaseUrl,
                        forcedChannel = Registry.stringValue(UNITY_CLI_CHANNEL_KEY),
                        download = { url ->
                            reporter.sizedStep(DOWNLOAD_WORK, UnityBundle.message("unity.agents.cli.downloading")) {
                                downloadInstaller(url, script)
                            }
                        },
                        // The script step names nothing, so the task shows its own title again.
                        runInstaller = { commandLine ->
                            reporter.sizedStep(INSTALLER_WORK) {
                                runner.runInstaller(commandLine, ::logProcessOutput)
                            }
                        },
                        findCli = { findLocalCopy(UnityCliEnvironment.getInstance()).copy != null },
                    )
                }
            }
            if (result.failure != null) {
                report(project, modality, result.failure, result.exitCode, UnityCliOperation.INSTALLING)
            }
            else {
                // No version: the confirmation names the terminal instead, and a second tier 1
                // pass to read a version the sentence never uses would be waste.
                confirm(project, UnityCliOperation.INSTALLING, version = null)
            }
        }
        finally {
            runningOperation.set(null)
        }
    }

    private suspend fun runUpdate(project: Project, modality: ModalityState) {
        try {
            lastFailure.set(null)
            val state = detect()
            val commandLine = state.location?.let {
                unityCliUpdateCommandLine(state.installMethod, it, state.updateCommand)
            }
            if (commandLine == null) {
                logger.warn(
                    "Rider will not update a ${state.installMethod} copy of the Unity CLI at " +
                    "${state.location} with the command '${state.updateCommand}'"
                )
                report(project, modality, UnityCliFailure.LAUNCH_FAILED, null, UnityCliOperation.UPDATING)
                return
            }

            val exitCode = withBackgroundProgress(project, UnityBundle.message("unity.agents.cli.progress.update")) {
                UnityCliInstallerRunner.getInstance().runInstaller(commandLine, ::logProcessOutput)
            }
            val failure = unityCliUpdateOutcome(exitCode)
            if (failure != null) {
                report(project, modality, failure, exitCode, UnityCliOperation.UPDATING)
            }
            else {
                val version = findLocalCopy(UnityCliEnvironment.getInstance()).copy?.version
                confirm(project, UnityCliOperation.UPDATING, version)
            }
        }
        finally {
            runningOperation.set(null)
        }
    }

    private suspend fun downloadInstaller(url: String, script: UnityCliInstallerScript): Path? {
        return try {
            withContext(Dispatchers.IO) {
                val installer = Files.createTempFile("unity-cli-", "-${script.fileName}")
                HttpRequests.request(url).productNameAsUserAgent().saveToFile(installer, null)
                installer
            }
        }
        catch (e: CancellationException) {
            throw e
        }
        catch (e: IOException) {
            logger.warn("Rider could not download the installer script of Unity from $url", e)
            null
        }
    }

    private fun logProcessOutput(text: String) {
        val line = text.trimEnd('\n', '\r')
        if (line.isNotBlank()) logger.info("Unity CLI: $line")
    }

    @Suppress("DialogTitleCapitalization")
    private suspend fun report(
        project: Project,
        modality: ModalityState,
        failure: UnityCliFailure,
        exitCode: Int?,
        operation: UnityCliOperation,
    ) {
        val record = FailureRecord(failureIds.incrementAndGet(), failure, exitCode, operation)
        lastFailure.set(record)

        val pageIsShowing = withContext(Dispatchers.EDT + modality.asContextElement()) {
            UnityForAgentsPageVisibility.getInstance().isAnyPageShowing()
        }
        if (pageIsShowing) return

        val title = when (operation) {
            UnityCliOperation.INSTALLING -> UnityBundle.message("unity.agents.cli.error.title.install")
            UnityCliOperation.UPDATING -> UnityBundle.message("unity.agents.cli.error.title.update")
        }

        val notification = NotificationGroupManager.getInstance()
            .getNotificationGroup(UNITY_AGENTS_ERRORS_GROUP)
            .createNotification(title, unityCliFailureMessage(failure, exitCode), NotificationType.ERROR)
        notification.setDisplayId(
            when (operation) {
                UnityCliOperation.INSTALLING -> FAILED_INSTALL_DISPLAY_ID
                UnityCliOperation.UPDATING -> FAILED_UPDATE_DISPLAY_ID
            }
        )
        notification.setSuggestionType(true)
        notification.addAction(
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.cli.try.again")) {
                clearFailure(record.id)
                when (operation) {
                    UnityCliOperation.INSTALLING -> requestInstall(project, ModalityState.nonModal())
                    UnityCliOperation.UPDATING -> requestUpdate(project, ModalityState.nonModal())
                }
            }
        )
        // The balloon fires only when no page shows, so without this link its reader cannot reach
        // the page that carries the same failure and the rest of the state.
        notification.addAction(
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.open.settings")) {
                openUnityForAgentsPage(project)
            }
        )

        if (ShowLogAction.isSupported()) {
            notification.addAction(ShowLogAction.notificationAction())
        }
        notification.notify(project)
    }

    private fun confirm(
        project: Project,
        operation: UnityCliOperation,
        version: String?,
    ) {
        val notification = NotificationGroupManager.getInstance()
            .getNotificationGroup(UNITY_AGENTS_CONFIRMATIONS_GROUP)
            .createNotification(
                unityCliConfirmationTitle(operation),
                unityCliConfirmationMessage(operation, version),
                NotificationType.INFORMATION,
            )
        notification.setDisplayId(
            when (operation) {
                UnityCliOperation.INSTALLING -> INSTALLED_DISPLAY_ID
                UnityCliOperation.UPDATING -> UPDATED_DISPLAY_ID
            }
        )
        notification.setIcon(AllIcons.Status.Success)
        notification.setSuggestionType(true)
        notification.addAction(
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.open.settings")) {
                openUnityForAgentsPage(project)
            }
        )
        notification.notify(project)
    }

    // The lookup reads the PATH, the file system and the link targets, so it runs off the default dispatcher.
    private suspend fun findLocalCopy(environment: UnityCliEnvironment): LocalLookup = withContext(Dispatchers.IO) {
        val onPath = environment.findOnPath(UNITY_CLI_COMMAND)
        if (onPath != null) {
            if (isUnityHubPrivateCopy(onPath)) return@withContext LocalLookup(foreignBinaryOnPath = onPath)
            val version = identify(onPath)
            return@withContext if (version == null) LocalLookup(foreignBinaryOnPath = onPath)
            else LocalLookup(copy = LocalCopy(onPath, version))
        }

        val candidates = unityCliWellKnownCandidates(
            unityCliHome = environment.unityCliHome(),
            localAppData = environment.localAppData(),
            userHome = environment.userHome(),
            executableName = unityCliExecutableName(),
        )
        for (candidate in candidates) {
            if (isUnityHubPrivateCopy(candidate)) continue
            if (!environment.isExecutable(candidate)) continue
            val version = identify(candidate) ?: continue
            return@withContext LocalLookup(copy = LocalCopy(candidate, version))
        }
        LocalLookup()
    }

    private suspend fun identify(candidate: Path): String? {
        val version = runQuietly(candidate) { runner ->
            runner.runProcess(unityCliVersionCommandLine(candidate), UnityCliProcessRunner.IDENTIFY_TIMEOUT_MS)
        } ?: return null
        if (version.isTimeout || version.exitCode != 0 || !isUnityCliVersionOutput(version.stdout)) return null

        val help = runQuietly(candidate) { runner ->
            runner.runProcess(unityCliHelpCommandLine(candidate), UnityCliProcessRunner.IDENTIFY_TIMEOUT_MS)
        } ?: return null
        if (help.isTimeout || help.exitCode != 0 || !isUnityCliHelpOutput(help.stdout)) return null

        return version.stdout.trim()
    }

    private suspend fun readUpdateInfo(copy: LocalCopy): CachedUpdateInfo {
        val key = CacheKey(copy.path, copy.version)
        freshCache(key)?.let { return it }
        return updateInfoLock.withLock {
            freshCache(key) ?: readUpdateInfoFromCli(key).also { cachedUpdateInfo = it }
        }
    }

    private fun freshCache(key: CacheKey): CachedUpdateInfo? =
        cachedUpdateInfo?.takeIf { it.key == key && System.currentTimeMillis() - it.readAt < UPDATE_INFO_TTL_MS }

    private suspend fun readUpdateInfoFromCli(key: CacheKey): CachedUpdateInfo {
        val output = runQuietly(key.path) { runner ->
            runner.runProcess(unityCliDiagnoseUpdateCommandLine(key.path), UnityCliProcessRunner.DIAGNOSE_TIMEOUT_MS)
        }
        val info = output
            ?.takeIf { !it.isTimeout && it.exitCode == 0 }
            ?.let { parseUnityCliUpdateInfo(it.stdout) }
        val latestVersion = info?.manifestUrl?.let { readLatestVersion(it) }
        return CachedUpdateInfo(key, System.currentTimeMillis(), info, latestVersion)
    }

    private suspend fun readLatestVersion(manifestUrl: String): String? {
        if (!isReadableManifestUrl(manifestUrl)) {
            logger.warn("The Unity CLI named a manifest that Rider will not read: '$manifestUrl'")
            return null
        }
        return withContext(Dispatchers.IO) {
            try {
                val bytes = HttpRequests.request(manifestUrl).connect { request ->
                    request.inputStream.readNBytes(MANIFEST_BODY_CAP_BYTES + 1)
                }
                if (bytes.size > MANIFEST_BODY_CAP_BYTES) {
                    logger.warn("The channel manifest of the Unity CLI at $manifestUrl is larger than $MANIFEST_BODY_CAP_BYTES bytes")
                    null
                }
                else {
                    parseUnityCliManifestVersion(String(bytes, Charsets.UTF_8))
                }
            }
            catch (e: IOException) {
                logger.debug("The channel manifest of the Unity CLI at $manifestUrl is unreadable", e)
                null
            }
        }
    }

    private suspend fun installMethodOf(
        environment: UnityCliEnvironment,
        copy: LocalCopy,
        info: UnityCliUpdateInfo?,
    ): UnityCliInstallMethod {
        // realPath reads the link target from the disk.
        val realPath = withContext(Dispatchers.IO) { environment.realPath(copy.path) }
        val fromPath = unityCliInstallMethodFromPath(realPath)
        if (fromPath != UnityCliInstallMethod.UNKNOWN) return fromPath
        return info?.installMethod ?: UnityCliInstallMethod.UNKNOWN
    }

    private suspend fun runQuietly(
        candidate: Path,
        launch: suspend (UnityCliProcessRunner) -> ProcessOutput,
    ): ProcessOutput? =
        try {
            launch(UnityCliProcessRunner.getInstance())
        }
        catch (e: ExecutionException) {
            logger.debug("Rider could not launch $candidate", e)
            null
        }
        catch (e: IOException) {
            logger.debug("Rider could not launch $candidate", e)
            null
        }

    private data class LocalCopy(val path: Path, val version: String)

    private data class LocalLookup(val copy: LocalCopy? = null, val foreignBinaryOnPath: Path? = null)

    private data class CacheKey(val path: Path, val version: String)

    private data class FailureRecord(
        val id: Long,
        val failure: UnityCliFailure,
        val exitCode: Int?,
        val operation: UnityCliOperation,
    )

    private data class CachedUpdateInfo(
        val key: CacheKey,
        val readAt: Long,
        val info: UnityCliUpdateInfo?,
        val latestVersion: String?,
    )

    private companion object {
        val logger: Logger = Logger.getInstance(UnityCliStatusService::class.java)

        const val UNITY_AGENTS_ERRORS_GROUP = "Unity for Agents Errors"
        const val UNITY_AGENTS_CONFIRMATIONS_GROUP = "Unity for Agents Confirmations"
        const val FAILED_INSTALL_DISPLAY_ID = "unity.for.agents.cli.install.failed"
        const val FAILED_UPDATE_DISPLAY_ID = "unity.for.agents.cli.update.failed"
        const val INSTALLED_DISPLAY_ID = "unity.for.agents.cli.installed"
        const val UPDATED_DISPLAY_ID = "unity.for.agents.cli.updated"
        const val DOWNLOAD_WORK = 20
        const val INSTALLER_WORK = 80
        const val MANIFEST_BODY_CAP_BYTES = 64 * 1024
        const val UNITY_CLI_CHANNEL_KEY = "rider.unity.agents.cli.channel"
        val UPDATE_INFO_TTL_MS: Long = TimeUnit.HOURS.toMillis(6)
    }
}
