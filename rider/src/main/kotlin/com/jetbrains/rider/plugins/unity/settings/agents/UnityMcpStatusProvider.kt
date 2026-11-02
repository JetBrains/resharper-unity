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
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.projectDir
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext
import java.nio.file.Files
import java.nio.file.Path
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.atomic.AtomicLong
import kotlin.io.path.readText

internal interface UnityMcpStatusProvider {
    suspend fun read(cli: UnityCliPresentation): UnityMcpSectionPresentation

    suspend fun write(
        stores: List<UnityMcpStore>,
        operation: UnityMcpOperation,
        modality: ModalityState,
        packageInstallAgreed: Boolean = false,
        networkPolicyRemovalAgreed: Boolean = false,
    )

    companion object {
        fun getInstance(project: Project): UnityMcpStatusProvider = project.service<UnityMcpStatusService>()
    }
}

internal suspend fun readUnityCli(project: Project): UnityCliPresentation =
    if (TrustedProjects.isProjectTrusted(project)) UnityCliStatusProvider.getInstance().read()
    else UnityCliPresentation(projectTrusted = false)

internal fun unityMcpProject(project: Project): UnityMcpProject? {
    if (project.isDefault || !project.isUnityProject.value) return null
    val path = unitySolutionDirectory(project)?.toString() ?: return null
    return UnityMcpProject(project.name, path)
}

internal fun unitySolutionDirectory(project: Project): Path? =
    try {
        project.projectDir.toNioPath()
    }
    catch (e: Throwable) {
        rethrowControlFlowException(e)
        logger.debug("Rider could not read the solution directory of the project", e)
        null
    }

private val logger: Logger = Logger.getInstance(UnityMcpStatusService::class.java)

@Service(Service.Level.PROJECT)
internal class UnityMcpStatusService(
    private val project: Project,
    private val scope: CoroutineScope,
) : UnityMcpStatusProvider {
    private val heldRows = ConcurrentHashMap<UnityMcpStore, HeldRow>()
    private val claimLock = Mutex()
    private val claimCounter = AtomicLong()
    private val releaseCounter = AtomicLong()

    override suspend fun read(cli: UnityCliPresentation): UnityMcpSectionPresentation {
        val readStart = releaseCounter.get()
        val unityProject = unityMcpProject(project)
        val section = unityMcpSectionState(cli, unityProject != null)
        val unity = cli.location
        val solution = unityProject?.let { Path.of(it.path) }
        if (!section.canAct || unity == null || solution == null) {
            val rows = withHeldState(gatedRows(section), readStart)
            return UnityMcpSectionPresentation(
                cli = cli,
                project = unityProject,
                rows = rows,
            )
        }

        return coroutineScope {
            val storeFiles = async { readJsonStores(UnityMcpStore.entries, solution) }
            val codexStoreFile = async { readStoreFile(unityMcpCodexStoreFile(solution)) }

            val readings = storeFiles.await()
            val rows = withHeldState(
                UnityMcpStore.entries.map { store ->
                    when (store) {
                        UnityMcpStore.CODEX_CLI -> codexRow(section, unityMcpCodexStoreFile(solution), codexStoreFile.await(), unity)
                        else -> jsonStoreRow(store, section, readings, solution, unity)
                    }
                },
                readStart,
            )
            UnityMcpSectionPresentation(
                cli = cli,
                project = unityProject,
                rows = rows,
            )
        }
    }

    override suspend fun write(
        stores: List<UnityMcpStore>,
        operation: UnityMcpOperation,
        modality: ModalityState,
        packageInstallAgreed: Boolean,
        networkPolicyRemovalAgreed: Boolean,
    ) {
        scope.launch(modality.asContextElement()) {
            claimAndWrite(stores, operation, packageInstallAgreed, networkPolicyRemovalAgreed)
        }.join()
    }

    private suspend fun claimAndWrite(
        stores: List<UnityMcpStore>,
        operation: UnityMcpOperation,
        packageInstallAgreed: Boolean,
        networkPolicyRemovalAgreed: Boolean,
    ) {
        if (!UnityForAgentsFeature.isMcpSectionEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return

        val token = claimCounter.incrementAndGet()
        val claimed = claimWriteGroups(stores, operation, token)
        if (claimed.isEmpty()) return
        try {
            val title = when (operation) {
                UnityMcpOperation.CONFIGURING -> UnityBundle.message("unity.agents.mcp.progress.configure")
                UnityMcpOperation.REMOVING -> UnityBundle.message("unity.agents.mcp.progress.remove")
            }
            withBackgroundProgress(project, title) {
                val unity = UnityCliStatusProvider.getInstance().read().location
                val solution = unitySolutionDirectory(project)
                val before = readEntries(claimed, unity, solution)
                val outcome = when (operation) {
                    UnityMcpOperation.CONFIGURING ->
                        UnityPipelineStatusProvider.getInstance(project).ensureInstalled(packageInstallAgreed, UnityAgentsSource.SETTINGS)
                    UnityMcpOperation.REMOVING -> UnityPipelinePackageOutcome.Ready
                }
                val answers = when (outcome) {
                    UnityPipelinePackageOutcome.Ready -> runWrites(claimed, operation, unity, solution, networkPolicyRemovalAgreed)
                    UnityPipelinePackageOutcome.Declined -> return@withBackgroundProgress
                    is UnityPipelinePackageOutcome.Failed -> claimed.associateWith { WriteAnswer.Failed(outcome.failure) }
                }
                val after = readEntries(claimed, unity, solution)
                for ((store, answer) in answers) {
                    val confirmed = unityMcpWriteConfirmed(operation, after.getValue(store))
                    val failure = answer.failure(confirmed)
                    release(store, failure, before.getValue(store), after.getValue(store))
                    if (store in stores) {
                        val result = if (failure == null) UnityAgentsResult.SUCCESS else UnityAgentsResult.FAILED
                        UnityForAgentsUsagesCollector.logMcpOperationFinished(project, store, operation, result)
                    }
                }
            }
        }
        finally {
            claimed.forEach { releaseIfBusy(it, token) }
        }
    }

    private suspend fun runWrites(
        stores: List<UnityMcpStore>,
        operation: UnityMcpOperation,
        unity: Path?,
        solution: Path?,
        networkPolicyRemovalAgreed: Boolean,
    ): Map<UnityMcpStore, WriteAnswer> {
        if (unity == null || solution == null) {
            logger.warn("Rider cannot write an MCP entry without the Unity CLI and the solution directory")
            return stores.associateWith { WriteAnswer.Failed(UnityMcpFailure.FILE_WRITE) }
        }

        return coroutineScope {
            unityMcpWriteGroups(stores)
                .map { group -> async { group to runWrite(group.first(), operation, unity, solution, networkPolicyRemovalAgreed) } }
                .awaitAll()
        }.flatMap { (group, answer) -> group.map { it to answer } }.toMap()
    }

    private suspend fun readEntries(
        stores: List<UnityMcpStore>,
        unity: Path?,
        solution: Path?,
    ): Map<UnityMcpStore, UnityMcpEntry> {
        if (solution == null) return stores.associateWith { UnityMcpEntry.UNREADABLE }
        return coroutineScope {
            val codexStore = async {
                if (UnityMcpStore.CODEX_CLI in stores) readStoreFile(unityMcpCodexStoreFile(solution)) else StoreReading(exists = false)
            }
            val readings = readJsonStores(stores, solution)
            stores.associateWith { store ->
                when (store) {
                    UnityMcpStore.CODEX_CLI -> codexStore.await().let { readCodexEntry(it.content, it.exists, unity) }
                    else -> {
                        val reading = readings[unityMcpJsonStoreFile(store, solution)] ?: StoreReading(exists = false)
                        readMcpServersEntry(reading.content, reading.exists, unity)
                    }
                }
            }
        }
    }

    private suspend fun runWrite(
        store: UnityMcpStore,
        operation: UnityMcpOperation,
        unity: Path,
        solution: Path,
        networkPolicyRemovalAgreed: Boolean,
    ): WriteAnswer {
        if (store == UnityMcpStore.CODEX_CLI) {
            val file = unityMcpCodexStoreFile(solution)
            return when (operation) {
                UnityMcpOperation.CONFIGURING -> configureCodex(file, unity)
                UnityMcpOperation.REMOVING ->
                    WriteAnswer.Wrote(writeStoreFile(file) { removeUnityMcpCodexEntryInIde(file, networkPolicyRemovalAgreed) })
            }
        }
        val owned = unityMcpOwnedStore(store, unity, solution) ?: return WriteAnswer.Failed(UnityMcpFailure.FILE_WRITE)
        return runOwnWrite(owned, operation)
    }

    private suspend fun configureCodex(file: Path, unity: Path): WriteAnswer {
        var editable = true
        val written = writeStoreFile(file) {
            writeStoreInIde(file) { addUnityMcpCodexEntry(file, unity).also { editable = it } }
        }
        return if (editable) WriteAnswer.Wrote(written) else WriteAnswer.Failed(UnityMcpFailure.CODEX_STORE_SHAPE)
    }

    private suspend fun runOwnWrite(owned: UnityMcpOwnedStore, operation: UnityMcpOperation): WriteAnswer {
        val entry = when (operation) {
            UnityMcpOperation.CONFIGURING -> owned.entry
            UnityMcpOperation.REMOVING -> null
        }
        return WriteAnswer.Wrote(writeStoreFile(owned.file) { writeUnityMcpJsonStoreInIde(owned.file, entry) })
    }

    private suspend fun writeStoreFile(file: Path, write: suspend () -> Boolean): Boolean =
        try {
            write()
        }
        catch (e: Throwable) {
            rethrowControlFlowException(e)
            logger.warn("Rider could not write $file", e)
            false
        }

    private fun gatedRows(section: UnityMcpSectionState): List<UnityMcpRowPresentation> =
        UnityMcpStore.entries.map { UnityMcpRowPresentation(store = it, section = section) }

    private fun withHeldState(rows: List<UnityMcpRowPresentation>, readStart: Long): List<UnityMcpRowPresentation> =
        rows.map { row ->
            val held = heldRows[row.store] ?: return@map row
            if (held.operation == null && unityMcpHeldIsStale(held.writtenEntry, held.release, row.entry, readStart)) {
                heldRows.remove(row.store, held)
                return@map row
            }
            row.copy(
                restartNeeded = held.restartNeeded,
                operation = held.operation,
                failure = held.failure,
            )
        }

    private suspend fun claimWriteGroups(
        stores: List<UnityMcpStore>,
        operation: UnityMcpOperation,
        token: Long,
    ): List<UnityMcpStore> = claimLock.withLock {
        unityMcpWriteGroups(stores).filter { group -> claimGroup(group, operation, token) }.flatten()
    }

    private fun claimGroup(group: List<UnityMcpStore>, operation: UnityMcpOperation, token: Long): Boolean {
        val taken = group.takeWhile { claim(it, operation, token) }
        if (taken.size == group.size) return true
        taken.forEach { releaseIfBusy(it, token) }
        return false
    }

    private fun claim(store: UnityMcpStore, operation: UnityMcpOperation, token: Long): Boolean {
        var claimed = false
        heldRows.compute(store) { _, held ->
            if (held?.operation != null) return@compute held
            claimed = true
            // The click clears the failure the user saw, so the row leaves the failed state at once.
            HeldRow(
                restartNeeded = held?.restartNeeded == true,
                operation = operation,
                claim = token,
                writtenEntry = held?.writtenEntry,
                release = held?.release ?: 0,
            )
        }
        return claimed
    }

    private fun release(
        store: UnityMcpStore,
        failure: UnityMcpFailure?,
        before: UnityMcpEntry,
        after: UnityMcpEntry,
    ) {
        val release = releaseCounter.incrementAndGet()
        heldRows.compute(store) { _, held ->
            HeldRow(
                restartNeeded = unityMcpRestartNeeded(
                    failure = failure,
                    before = before,
                    after = after,
                    heldRestart = held?.restartNeeded == true,
                    heldEntry = held?.writtenEntry,
                ),
                failure = failure,
                writtenEntry = after,
                release = release,
            )
        }
    }

    private fun releaseIfBusy(store: UnityMcpStore, token: Long) {
        heldRows.computeIfPresent(store) { _, held ->
            if (held.claim != token) held else held.copy(operation = null, claim = null)
        }
    }

    private fun codexRow(
        section: UnityMcpSectionState,
        file: Path,
        storeFile: StoreReading,
        unity: Path,
    ): UnityMcpRowPresentation = UnityMcpRowPresentation(
        store = UnityMcpStore.CODEX_CLI,
        section = section,
        storePath = file.toString(),
        storeFileExists = storeFile.exists,
        entry = readCodexEntry(storeFile.content, storeFile.exists, unity),
        removalAsksForNetworkPolicy = storeFile.content?.let(::unityMcpCodexRemoval)?.asksForNetworkPolicy == true,
        configureReplacesOtherServer = unityMcpCodexConfigureReplacesOtherServer(storeFile.content, unity),
    )

    private fun jsonStoreRow(
        store: UnityMcpStore,
        section: UnityMcpSectionState,
        readings: Map<Path, StoreReading>,
        solution: Path,
        unity: Path,
    ): UnityMcpRowPresentation {
        val file = unityMcpJsonStoreFile(store, solution)
        val reading = readings[file] ?: StoreReading(exists = false)
        return UnityMcpRowPresentation(
            store = store,
            section = section,
            storePath = file?.toString(),
            storeFileExists = reading.exists,
            entry = readMcpServersEntry(reading.content, reading.exists, unity),
            configureReplacesOtherServer = unityMcpConfigureReplacesOtherServer(reading.content),
        )
    }

    private suspend fun readJsonStores(
        stores: List<UnityMcpStore>,
        solution: Path,
    ): Map<Path, StoreReading> = coroutineScope {
        stores
            .mapNotNull { unityMcpJsonStoreFile(it, solution) }
            .distinct()
            .map { file -> async { file to readStoreFile(file) } }
            .awaitAll()
            .toMap()
    }

    private suspend fun readStoreFile(file: Path): StoreReading = withContext(Dispatchers.IO) {
        try {
            if (Files.notExists(file)) StoreReading(exists = false)
            else StoreReading(exists = true, content = file.readText())
        }
        catch (e: Throwable) {
            rethrowControlFlowException(e)
            logger.debug("Rider could not read $file", e)
            StoreReading(exists = true)
        }
    }

    private data class HeldRow(
        val restartNeeded: Boolean = false,
        val operation: UnityMcpOperation? = null,
        val claim: Long? = null,
        val failure: UnityMcpFailure? = null,
        val writtenEntry: UnityMcpEntry? = null,
        val release: Long = 0,
    )

    private data class StoreReading(val exists: Boolean, val content: String? = null)

    private sealed interface WriteAnswer {
        data class Failed(val failure: UnityMcpFailure) : WriteAnswer
        data class Wrote(val written: Boolean) : WriteAnswer

        fun failure(confirmed: Boolean): UnityMcpFailure? = when (this) {
            is Failed -> failure
            is Wrote -> unityMcpFileWriteOutcome(written, confirmed)
        }
    }
}
