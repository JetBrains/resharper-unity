package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.execution.ExecutionException
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessOutput
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
import com.intellij.openapi.project.ProjectManager
import com.intellij.openapi.util.registry.Registry
import com.intellij.platform.ide.progress.withBackgroundProgress
import com.intellij.platform.util.progress.reportSequentialProgress
import com.intellij.util.io.HttpRequests
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.isUnityProject
import kotlinx.coroutines.CancellationException
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
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.TimeUnit
import java.util.concurrent.atomic.AtomicLong
import java.util.concurrent.atomic.AtomicReference

@ApiStatus.Internal
interface UnityCliStatusProvider {
    suspend fun read(): UnityCliPresentation
    suspend fun install(project: Project, modality: ModalityState, source: UnityAgentsSource)
    suspend fun update(project: Project, modality: ModalityState, source: UnityAgentsSource)
    fun requestInstall(project: Project, modality: ModalityState, source: UnityAgentsSource)
    fun requestUpdate(project: Project, modality: ModalityState, source: UnityAgentsSource)
    /**
     * Clears the failure with this id. A failure that arrived later stays, because the user did not see it.
     */
    fun clearFailure(failureId: Long)

    /**
     * Drops the cached versions. The next [read] runs `unity --version` and `unity --help`, and reads the channel manifest again.
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
    private val cachedVersions = ConcurrentHashMap<Path, CachedVersion>()
    private val runningOperation = AtomicReference<UnityCliOperation?>(null)
    private val lastFailure = AtomicReference<FailureRecord?>(null)
    private val failureIds = AtomicLong()

    override suspend fun read(): UnityCliPresentation = withHeldState(detect())

    override suspend fun install(project: Project, modality: ModalityState, source: UnityAgentsSource) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        if (!runningOperation.compareAndSet(null, UnityCliOperation.INSTALLING)) {
            logger.info("The Unity CLI install was asked for while ${runningOperation.get()} runs")
            return
        }
        scope.launch { runInstall(project, modality, source) }.join()
    }

    override suspend fun update(project: Project, modality: ModalityState, source: UnityAgentsSource) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return
        if (!runningOperation.compareAndSet(null, UnityCliOperation.UPDATING)) {
            logger.info("The Unity CLI update was asked for while ${runningOperation.get()} runs")
            return
        }
        scope.launch { runUpdate(project, modality, source) }.join()
    }

    override fun requestInstall(project: Project, modality: ModalityState, source: UnityAgentsSource) {
        scope.launch { install(project, modality, source) }
    }

    override fun requestUpdate(project: Project, modality: ModalityState, source: UnityAgentsSource) {
        scope.launch { update(project, modality, source) }
    }

    override fun forgetUpdateInfo() {
        cachedUpdateInfo = null
        cachedVersions.clear()
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

        return UnityCliPresentation(
            detected = true,
            installedVersion = copy.version,
            location = copy.path,
            installMethod = installMethodOf(environment, copy),
            latestVersion = readUpdateInfo(copy).latestVersion,
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

    private suspend fun runInstall(project: Project, modality: ModalityState, source: UnityAgentsSource) {
        try {
            val script = UnityCliInstallerScript.forCurrentOs()
            if (script == null) {
                logger.warn("Unity publishes no installer script for this operating system")
                report(project, modality, UnityCliFailure.DOWNLOAD, null, UnityCliOperation.INSTALLING, source)
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
                        findCli = { findCurrentVersion() != null },
                    )
                }
            }
            if (result.failure != null) {
                report(project, modality, result.failure, result.exitCode, UnityCliOperation.INSTALLING, source)
            }
            else {
                logResult(project, UnityCliOperation.INSTALLING, source, UnityAgentsResult.SUCCESS)
            }
        }
        finally {
            runningOperation.set(null)
        }
    }

    private suspend fun runUpdate(project: Project, modality: ModalityState, source: UnityAgentsSource) {
        try {
            lastFailure.set(null)
            val state = detect()
            val location = state.location
            val installedVersion = state.installedVersion
            val result = when {
                location == null || installedVersion == null -> null
                state.installMethod == UnityCliInstallMethod.CDN -> runCdnUpdate(project, location, installedVersion)
                else -> unityCliUpdateCommandLine(state.installMethod)?.let { runPackageManagerUpdate(project, it) }
            }
            when {
                result == null -> {
                    logger.warn("Rider will not update a ${state.installMethod} copy of the Unity CLI at ${state.location}")
                    report(project, modality, UnityCliFailure.LAUNCH_FAILED, null, UnityCliOperation.UPDATING, source)
                }
                result.failure != null -> report(project, modality, result.failure, result.exitCode, UnityCliOperation.UPDATING, source)
                else -> {
                    logResult(project, UnityCliOperation.UPDATING, source, UnityAgentsResult.SUCCESS)
                    refreshSkills(source)
                }
            }
        }
        finally {
            runningOperation.set(null)
        }
    }

    /** Installs the unity-cli skill again in each open Unity project that has it, so that the skill matches the new CLI. */
    private fun refreshSkills(source: UnityAgentsSource) {
        for (openProject in ProjectManager.getInstance().openProjects) {
            if (openProject.isDisposed || openProject.isDefault || !openProject.isUnityProject.value) continue
            UnitySkillStatusProvider.getInstance(openProject).requestRefresh(source)
        }
    }

    private suspend fun runPackageManagerUpdate(project: Project, commandLine: GeneralCommandLine): UnityCliInstallResult {
        val exitCode = withBackgroundProgress(project, UnityBundle.message("unity.agents.cli.progress.update")) {
            UnityCliInstallerRunner.getInstance().runInstaller(commandLine, ::logProcessOutput)
        }
        val failure = unityCliUpdateOutcome(exitCode) ?: UnityCliFailure.VERIFICATION.takeIf { findCurrentVersion() == null }
        return UnityCliInstallResult(failure, exitCode)
    }

    private suspend fun runCdnUpdate(project: Project, location: Path, installedVersion: String): UnityCliInstallResult {
        val runner = UnityCliInstallerRunner.getInstance()
        val channel = channelOf(installedVersion)
        return withBackgroundProgress(project, UnityBundle.message("unity.agents.cli.progress.update")) {
            runUnityCliUpdateChain(
                cliPath = location,
                readLatestVersion = { readLatestVersion(channel) },
                runCommand = { commandLine -> runner.runInstaller(commandLine, ::logProcessOutput) },
                findCli = { findCurrentVersion() },
            )
        }
    }

    /** Detects the CLI again after an install or an update, because both can replace the binary. */
    private suspend fun findCurrentVersion(): String? {
        forgetUpdateInfo()
        return findLocalCopy(UnityCliEnvironment.getInstance()).copy?.version
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

    private fun logResult(project: Project, operation: UnityCliOperation, source: UnityAgentsSource, result: UnityAgentsResult) {
        val agentsOperation = when (operation) {
            UnityCliOperation.INSTALLING -> UnityAgentsOperation.INSTALLING
            UnityCliOperation.UPDATING -> UnityAgentsOperation.UPDATING
        }
        UnityForAgentsUsagesCollector.logComponentOperationFinished(project, UnityAgentsSetupStep.CLI, agentsOperation, source, result)
    }

    @Suppress("DialogTitleCapitalization")
    private suspend fun report(
        project: Project,
        modality: ModalityState,
        failure: UnityCliFailure,
        exitCode: Int?,
        operation: UnityCliOperation,
        source: UnityAgentsSource,
    ) {
        logResult(project, operation, source, UnityAgentsResult.FAILED)
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
        notification.setAddExtraAction(true)
        notification.addAction(
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.cli.try.again")) {
                UnityForAgentsUsagesCollector.logActionClicked(project, UnityAgentsAction.RETRY, UnityAgentsSource.NOTIFICATION)
                clearFailure(record.id)
                when (operation) {
                    UnityCliOperation.INSTALLING -> requestInstall(project, ModalityState.nonModal(), UnityAgentsSource.NOTIFICATION)
                    UnityCliOperation.UPDATING -> requestUpdate(project, ModalityState.nonModal(), UnityAgentsSource.NOTIFICATION)
                }
            }
        )
        // The balloon fires only when no page shows, so without this link its reader cannot reach
        // the page that carries the same failure and the rest of the state.
        notification.addAction(
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.open.settings")) {
                UnityAgentsSetupProvider.getInstance(project).openSettings(UnityAgentsSource.NOTIFICATION)
            }
        )

        if (ShowLogAction.isSupported()) {
            notification.addAction(ShowLogAction.notificationAction())
        }
        notification.notify(project)
    }

    // The lookup reads the PATH, the file system and the link targets, so it runs off the default dispatcher.
    private suspend fun findLocalCopy(environment: UnityCliEnvironment): LocalLookup = withContext(Dispatchers.IO) {
        val onPath = environment.findOnPath(UNITY_CLI_COMMAND)
        if (onPath != null) {
            if (isUnityHubPrivateCopy(onPath)) return@withContext LocalLookup(foreignBinaryOnPath = onPath)
            val version = identify(onPath, environment)
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
            val version = identify(candidate, environment) ?: continue
            return@withContext LocalLookup(copy = LocalCopy(candidate, version))
        }
        LocalLookup()
    }

    private suspend fun identify(candidate: Path, environment: UnityCliEnvironment): String? {
        val stamp = environment.fileStamp(candidate)
        val now = environment.currentTimeMs()
        val cached = cachedVersions[candidate]
        if (stamp != null && cached != null && cached.stamp == stamp && now - cached.readAt < versionTtl(cached.version)) {
            return cached.version
        }

        val identity = readIdentity(candidate) ?: return null
        // A copy with no stamp gets no entry, so both commands run on each read.
        if (stamp != null) cachedVersions[candidate] = CachedVersion(stamp, identity.version, now)
        return identity.version
    }

    /** A version stays cached for hours. A program that is not the Unity CLI is checked again after a minute. */
    private fun versionTtl(version: String?): Long = if (version != null) VERSION_TTL_MS else NOT_CLI_TTL_MS

    /**
     * Runs `--version`, then `--help`, and gives the version of a Unity CLI.
     *
     * Rider runs `--help` only for a program that printed a version, so an unknown program gets no unknown arguments.
     * Gives null when a command timed out or did not start, so that the next read tries again.
     */
    private suspend fun readIdentity(candidate: Path): Identity? {
        val versionOutput = runIdentifyCommand(candidate, unityCliVersionCommandLine(candidate)) ?: return null
        val version = parseUnityCliVersion(versionOutput.stdout)?.takeIf { versionOutput.exitCode == 0 }
                      ?: return Identity(version = null)
        val helpOutput = runIdentifyCommand(candidate, unityCliHelpCommandLine(candidate)) ?: return null
        val isUnityCli = helpOutput.exitCode == 0 && isUnityCliHelpOutput(helpOutput.stdout)
        return Identity(version = version.takeIf { isUnityCli })
    }

    /** Runs [commandLine], and gives null when it timed out or did not start. */
    private suspend fun runIdentifyCommand(candidate: Path, commandLine: GeneralCommandLine): ProcessOutput? =
        runQuietly(candidate) { runner -> runner.runProcess(commandLine, UnityCliProcessRunner.IDENTIFY_TIMEOUT_MS) }
            ?.takeIf { !it.isTimeout }

    private suspend fun readUpdateInfo(copy: LocalCopy): CachedUpdateInfo {
        val key = CacheKey(copy.path, copy.version)
        freshCache(key)?.let { return it }
        return updateInfoLock.withLock {
            freshCache(key)
            ?: CachedUpdateInfo(key, System.currentTimeMillis(), readLatestVersion(channelOf(copy.version))).also { cachedUpdateInfo = it }
        }
    }

    private fun freshCache(key: CacheKey): CachedUpdateInfo? =
        cachedUpdateInfo?.takeIf { it.key == key && System.currentTimeMillis() - it.readAt < UPDATE_INFO_TTL_MS }

    private fun channelOf(installedVersion: String): String =
        unityCliChannel(Registry.stringValue(UNITY_CLI_CHANNEL_KEY), installedVersion)

    private suspend fun readLatestVersion(channel: String): String? {
        val manifestUrl = unityCliCdnUrl(UnityCliInstallerRunner.getInstance().cdnBaseUrl, unityCliChannelManifest(channel))
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

    // realPath reads the link targets from the disk.
    private suspend fun installMethodOf(environment: UnityCliEnvironment, copy: LocalCopy): UnityCliInstallMethod =
        withContext(Dispatchers.IO) {
            val cdnDirectories = unityCliCdnDirectories(environment.unityCliHome(), environment.localAppData(), environment.userHome())
            unityCliInstallMethodFromPath(
                path = copy.path,
                realPath = environment.realPath(copy.path),
                cdnDirectories = (cdnDirectories + cdnDirectories.map(environment.realPath)).distinct(),
                dpkgListsUnityCli = { dpkgListsUnityCli(DPKG_INFO) },
                dnfIsInstalled = { DNF_EXECUTABLES.any { Files.isRegularFile(it) } },
            )
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

    private data class CachedUpdateInfo(val key: CacheKey, val readAt: Long, val latestVersion: String?)

    private data class CachedVersion(val stamp: UnityCliFileStamp, val version: String?, val readAt: Long)

    /**
     * The answer of the two commands. A null [version] means that the program is not the Unity CLI.
     */
    private data class Identity(val version: String?)

    private companion object {
        val logger: Logger = Logger.getInstance(UnityCliStatusService::class.java)

        const val UNITY_AGENTS_ERRORS_GROUP = "Unity for Agents Errors"
        const val FAILED_INSTALL_DISPLAY_ID = "unity.for.agents.cli.install.failed"
        const val FAILED_UPDATE_DISPLAY_ID = "unity.for.agents.cli.update.failed"
        const val DOWNLOAD_WORK = 20
        const val INSTALLER_WORK = 80
        const val MANIFEST_BODY_CAP_BYTES = 64 * 1024
        const val UNITY_CLI_CHANNEL_KEY = "rider.unity.agents.cli.channel"
        val UPDATE_INFO_TTL_MS: Long = TimeUnit.HOURS.toMillis(6)

        /** A wrapper script keeps its stamp when the binary behind it changes, so a version expires too. */
        val VERSION_TTL_MS: Long = TimeUnit.HOURS.toMillis(6)

        /** A copy that failed once can answer on the next run, for example after its first start. */
        val NOT_CLI_TTL_MS: Long = TimeUnit.MINUTES.toMillis(1)
        val DPKG_INFO: Path = Path.of("/var/lib/dpkg/info")
        val DNF_EXECUTABLES: List<Path> = listOf(Path.of("/usr/bin/dnf"), Path.of("/usr/bin/dnf5"))
    }
}
