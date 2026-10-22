package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.icons.AllIcons
import com.intellij.ide.DataManager
import com.intellij.ide.actions.ShowLogAction
import com.intellij.ide.trustedProjects.TrustedProjectsDialog
import com.intellij.openapi.Disposable
import com.intellij.openapi.application.ApplicationActivationListener
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.application.EDT
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.application.asContextElement
import com.intellij.openapi.ide.CopyPasteManager
import com.intellij.openapi.options.SearchableConfigurable
import com.intellij.openapi.options.ex.Settings
import com.intellij.openapi.ui.MessageDialogBuilder
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.wm.IdeFrame
import com.intellij.platform.util.coroutines.childScope
import com.intellij.ui.AnimatedIcon
import com.intellij.ui.dsl.builder.AlignX
import com.intellij.ui.dsl.builder.MAX_LINE_LENGTH_NO_WRAP
import com.intellij.ui.dsl.builder.Panel
import com.intellij.ui.dsl.builder.Placeholder
import com.intellij.ui.dsl.builder.RightGap
import com.intellij.ui.dsl.builder.panel
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.UnityPluginScopeService
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.async
import kotlinx.coroutines.cancel
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.awt.datatransfer.StringSelection
import java.awt.event.HierarchyEvent
import java.nio.file.Path
import javax.swing.JComponent
import javax.swing.JProgressBar
import kotlin.time.Duration.Companion.seconds

internal class UnityForAgentsConfigurable(private val project: Project) : SearchableConfigurable {
    private data class UnityAgentsPagePresentation(
        val cli: UnityCliPresentation,
        val pipeline: UnityPipelinePresentation,
        val skill: UnitySkillPresentation,
        /** Null when the Unity MCP section is switched off. */
        val mcp: UnityMcpSectionPresentation?,
    )

    private var scope: CoroutineScope? = null
    private var refreshJob: Job? = null
    private var cliStatus: Placeholder? = null
    private var pipelineStatus: Placeholder? = null
    private var pipelinePanel: UnityPipelineSectionPanel? = null
    private var skillStatus: Placeholder? = null
    private var skillPanel: UnitySkillSectionPanel? = null
    private var mcpStatus: Placeholder? = null
    private var mcpPanel: UnityMcpSectionPanel? = null
    private var lastPage: UnityAgentsPagePresentation? = null
    // Not lastPage: a write, an install and an update paint a busy state that no read returned.
    private var paintedPage: UnityAgentsPagePresentation? = null
    private var root: JComponent? = null
    private var pageDisposable: Disposable? = null

    override fun getId(): String = UNITY_FOR_AGENTS_PAGE_ID

    override fun getDisplayName(): String = UnityBundle.message("configurable.name.unity.for.agents")

    override fun getHelpTopic(): String = UNITY_FOR_AGENTS_HELP_TOPIC

    override fun isModified(): Boolean = false

    override fun apply() {}

    override fun reset() {
        refresh()
    }

    override fun createComponent(): JComponent {
        val newScope = UnityPluginScopeService.getScope().childScope("UnityForAgentsConfigurable")
        scope = newScope

        val root = panel {
            group(UnityBundle.message("unity.agents.cli.title"), indent = false) {
                row { comment(UnityBundle.message("unity.agents.cli.description")) }
                indent {
                    row {
                        cliStatus = placeholder().align(AlignX.FILL)
                    }
                }
            }

            group(UnityBundle.message("unity.agents.pipeline.title"), indent = false) {
                row { comment(UnityBundle.message("unity.agents.pipeline.description")) }
                indent {
                    row {
                        pipelineStatus = placeholder().align(AlignX.FILL)
                    }
                }
            }

            group(UnityBundle.message("unity.agents.skills.title"), indent = false) {
                row { comment(UnityBundle.message("unity.agents.skills.description")) }
                indent {
                    row {
                        skillStatus = placeholder().align(AlignX.FILL)
                    }
                }
            }

            if (UnityForAgentsFeature.isMcpSectionEnabled()) {
                group(UnityBundle.message("unity.agents.mcp.title"), indent = false) {
                    row { comment(UnityBundle.message("unity.agents.mcp.description")) }
                    indent {
                        row {
                            mcpStatus = placeholder().align(AlignX.FILL)
                        }
                    }
                }
            }
        }

        this.pipelinePanel = UnityPipelineSectionPanel(
            install = {
                logClick(UnityAgentsAction.INSTALL)
                startPipeline(UnityPipelineOperation.INSTALLING)
            },
            upgrade = {
                logClick(UnityAgentsAction.UPDATE)
                startPipeline(UnityPipelineOperation.UPGRADING)
            },
            retry = {
                logClick(UnityAgentsAction.RETRY)
                retryPipeline(it)
            },
        )
        this.skillPanel = UnitySkillSectionPanel(
            install = {
                logClick(UnityAgentsAction.INSTALL)
                startSkillInstall(it)
            },
            retry = {
                logClick(UnityAgentsAction.RETRY)
                retrySkill()
            },
            openSkillsSettings = { openSkillsSettings(it) },
            browseSkills = {
                logClick(UnityAgentsAction.BROWSE_SKILLS)
                openSkillsSettings(it)
            },
        )
        this.mcpPanel = UnityMcpSectionPanel(project, write = { stores, operation -> startWrite(stores, operation) })
        this.root = root
        UnityForAgentsPageVisibility.getInstance().register(root)
        watchForOutsideChanges(root)

        refresh()
        return root
    }

    override fun disposeUIResources() {
        scope?.cancel()
        scope = null
        refreshJob = null
        cliStatus = null
        pipelineStatus = null
        pipelinePanel = null
        skillStatus = null
        skillPanel = null
        mcpStatus = null
        mcpPanel = null
        lastPage = null
        paintedPage = null
        pageDisposable?.let(Disposer::dispose)
        pageDisposable = null
        root?.let { UnityForAgentsPageVisibility.getInstance().unregister(it) }
        root = null
    }

    private fun watchForOutsideChanges(root: JComponent) {
        pageDisposable?.let(Disposer::dispose)
        val disposable = Disposer.newDisposable("Unity for Agents page")
        pageDisposable = disposable

        val connection = ApplicationManager.getApplication().messageBus.connect(disposable)
        connection.subscribe(ApplicationActivationListener.TOPIC, object : ApplicationActivationListener {
            override fun applicationActivated(ideFrame: IdeFrame) {
                if (root.isShowing) refresh()
            }
        })

        root.addHierarchyListener { event ->
            if (event.changeFlags and HierarchyEvent.SHOWING_CHANGED.toLong() != 0L && root.isShowing) {
                refresh()
            }
        }
    }

    private fun refresh() {
        val activeScope = scope ?: return
        val modality = ModalityState.current()
        val drawsMcp = mcpStatus != null
        // A slow read must not paint over the answer of a later one.
        refreshJob?.cancel()
        refreshJob = activeScope.launch {
            val cli = readUnityCli(project)
            val page = coroutineScope {
                val pipelineDeferred = async { UnityPipelineStatusProvider.getInstance(project).read(cli) }
                val mcp = if (drawsMcp) async { UnityMcpStatusProvider.getInstance(project).read(cli) } else null
                val pipeline = pipelineDeferred.await()
                val skill = UnitySkillStatusProvider.getInstance(project).read(cli, pipeline)
                UnityAgentsPagePresentation(cli, pipeline, skill, mcp?.await())
            }
            withContext(Dispatchers.EDT + modality.asContextElement()) {
                lastPage = page
                if (page == paintedPage) return@withContext
                paintedPage = page
                cliStatus?.component = buildCliStatus(page.cli)
                pipelineStatus?.component = pipelinePanel?.build(page.pipeline)
                skillStatus?.component = skillPanel?.build(page.skill)
                page.mcp?.let { mcpStatus?.component = mcpPanel?.build(it, needsPipelinePackage()) }
            }
            if (page.cli.operation != null || page.pipeline.operation != null || page.skill.installing) {
                delay(BUSY_REFRESH_INTERVAL)
                withContext(Dispatchers.EDT + modality.asContextElement()) { refresh() }
            }
        }
    }

    private fun startWrite(stores: List<UnityMcpStore>, operation: UnityMcpOperation) {
        if (!UnityForAgentsFeature.isMcpSectionEnabled()) return
        val activeScope = scope ?: return
        if (operation == UnityMcpOperation.REMOVING && !confirmedSharedRemoval(stores)) return
        val packageInstallAgreed = operation == UnityMcpOperation.CONFIGURING && agreedToPipelinePackage()
        if (operation == UnityMcpOperation.CONFIGURING && needsPipelinePackage() && !packageInstallAgreed) return
        if (packageInstallAgreed) logClick(UnityAgentsAction.INSTALL)

        val modality = ModalityState.current()

        refreshJob?.cancel()
        paintedPage = null
        lastPage?.mcp?.let { mcpStatus?.component = mcpPanel?.build(it.withRunning(stores, operation), needsPipelinePackage()) }
        if (packageInstallAgreed) paintPipelineBusy(UnityPipelineOperation.INSTALLING)
        activeScope.launch {
            UnityMcpStatusProvider.getInstance(project).write(stores, operation, modality, packageInstallAgreed)
            withContext(Dispatchers.EDT + modality.asContextElement()) { refresh() }
        }
    }

    private fun needsPipelinePackage(): Boolean = lastPage?.pipeline?.needsPipelinePackage == true

    private fun agreedToPipelinePackage(): Boolean {
        if (!needsPipelinePackage()) return false
        val manifest = lastPage?.pipeline?.manifestPath ?: return false
        return MessageDialogBuilder
            .yesNo(UnityBundle.message("unity.agents.mcp.pipeline.confirm.title"), unityMcpPipelineConfirmation(manifest))
            .yesText(UnityBundle.message("unity.agents.mcp.pipeline.confirm.yes"))
            .ask(project)
    }

    private fun confirmedSharedRemoval(stores: List<UnityMcpStore>): Boolean {
        val solution = lastPage?.mcp?.project?.path?.let { Path.of(it) }
        val question = unityMcpSharedRemovalQuestion(stores, solution) ?: return true
        return MessageDialogBuilder
            .yesNo(UnityBundle.message("unity.agents.mcp.remove.shared.title"), question)
            .yesText(UnityBundle.message("unity.agents.mcp.action.remove"))
            .ask(project)
    }

    /**
     * Draws every row the write is about to take, and not only the row the user clicked.
     *
     * The service claims a shared store file whole, so a click on the Claude Code row also takes the
     * GitHub Copilot CLI row. A redraw that covered the click alone left the second row with an
     * enabled button that the next click could not use.
     */
    private fun UnityMcpSectionPresentation.withRunning(
        stores: List<UnityMcpStore>,
        operation: UnityMcpOperation,
    ): UnityMcpSectionPresentation {
        val running = unityMcpWriteGroups(stores).flatten().toSet()
        return copy(
            rows = rows.map {
                if (it.store in running) it.copy(operation = operation, failure = null, failureExitCode = null) else it
            }
        )
    }

    private fun startPipeline(operation: UnityPipelineOperation) {
        if (!UnityForAgentsFeature.isEnabled()) return
        val activeScope = scope ?: return
        val modality = ModalityState.current()

        refreshJob?.cancel()
        paintedPage = null
        paintPipelineBusy(operation)
        activeScope.launch {
            val provider = UnityPipelineStatusProvider.getInstance(project)
            when (operation) {
                UnityPipelineOperation.INSTALLING -> provider.install(modality, UnityAgentsSource.SETTINGS)
                UnityPipelineOperation.UPGRADING -> provider.upgrade(modality, UnityAgentsSource.SETTINGS)
            }
            withContext(Dispatchers.EDT + modality.asContextElement()) { refresh() }
        }
    }

    private fun retryPipeline(presentation: UnityPipelinePresentation) {
        UnityPipelineStatusProvider.getInstance(project).clearFailure()
        startPipeline(presentation.failedOperation ?: UnityPipelineOperation.INSTALLING)
    }

    private fun paintPipelineBusy(operation: UnityPipelineOperation) {
        val pipeline = lastPage?.pipeline ?: return
        pipelineStatus?.component = pipelinePanel?.build(pipeline.copy(operation = operation, failure = null))
    }

    private fun startSkillInstall(target: UnitySkillTarget) {
        if (!UnityForAgentsFeature.isEnabled()) return
        val activeScope = scope ?: return
        val modality = ModalityState.current()

        refreshJob?.cancel()
        paintedPage = null
        val skill = lastPage?.skill
        if (skill != null) {
            skillStatus?.component = skillPanel?.build(skill.copy(installing = true, failure = null))
        }
        activeScope.launch {
            UnitySkillStatusProvider.getInstance(project).install(target, modality, UnityAgentsSource.SETTINGS)
            withContext(Dispatchers.EDT + modality.asContextElement()) { refresh() }
        }
    }

    private fun retrySkill() {
        UnitySkillStatusProvider.getInstance(project).clearFailure()
        startSkillInstall(UnitySkillTarget.ALL)
    }

    private fun openSkillsSettings(component: JComponent) {
        val settings = Settings.KEY.getData(DataManager.getInstance().getDataContext(component))
        if (settings != null) {
            val page = settings.find(AGENT_SKILLS_SETTINGS_ID)
            if (page != null) {
                settings.select(page, UNITY_SKILLS_SEARCH)
            }
        }
        else {
            openUnitySkillsPage(project)
        }
    }

    private fun logClick(action: UnityAgentsAction) {
        UnityForAgentsUsagesCollector.logActionClicked(project, action, UnityAgentsSource.SETTINGS)
    }

    private fun buildCliStatus(presentation: UnityCliPresentation): JComponent = panel {
        statusRows(presentation)
    }

    private fun Panel.statusRows(presentation: UnityCliPresentation) {
        installedRow(presentation)
        actionRow(presentation)
    }

    private fun Panel.installedRow(presentation: UnityCliPresentation) {
        val version = presentation.installedVersion ?: return
        val location = presentation.location ?: return
        if (presentation.offer == UnityCliOffer.CHECKING || presentation.offer == UnityCliOffer.NOT_TRUSTED) return
        row {
            icon(AllIcons.General.InspectionsOK).gap(RightGap.SMALL)
            label(UnityBundle.message("unity.agents.cli.installed"))
                .applyToComponent { foreground = UIUtil.getLabelSuccessForeground() }
            text(unityCliVersionMessage(version), MAX_LINE_LENGTH_NO_WRAP)
            text(unityCliLocationText(location), MAX_LINE_LENGTH_NO_WRAP)
        }
    }

    private fun Panel.actionRow(presentation: UnityCliPresentation) {
        when (presentation.offer) {
            UnityCliOffer.CHECKING -> row {
                icon(AnimatedIcon.Default.INSTANCE).gap(RightGap.SMALL)
                comment(UnityBundle.message("unity.agents.cli.checking"))
            }

            UnityCliOffer.NOT_TRUSTED -> row {
                button(UnityBundle.message("unity.agents.cli.trust.project")) { trustProject() }.gap(RightGap.SMALL)
                comment(UnityBundle.message("unity.agents.cli.not.trusted"))
            }

            UnityCliOffer.INSTALL -> {
                row {
                    button(UnityBundle.message("unity.agents.cli.install")) {
                        logClick(UnityAgentsAction.INSTALL)
                        startInstall()
                    }
                }
                // Rider runs a script of a third party, so the page says so before the user clicks.
                row { comment(unityCliInstallDisclosure(presentation.foreignBinaryOnPath)) }
            }

            UnityCliOffer.CURRENT -> {}

            UnityCliOffer.LATEST_VERSION_UNKNOWN -> row {
                button(UnityBundle.message("unity.agents.cli.check.again")) { recheck() }.gap(RightGap.SMALL)
                comment(UnityBundle.message("unity.agents.cli.latest.unknown"))
            }

            UnityCliOffer.UPDATE -> row {
                button(UnityBundle.message("unity.agents.cli.update")) {
                    logClick(UnityAgentsAction.UPDATE)
                    startUpdate()
                }.gap(RightGap.SMALL)
                comment(unityCliNewVersionMessage(presentation.latestVersion))
            }

            UnityCliOffer.SHOW_UPDATE_COMMAND -> {
                val command = unityCliUpdateCommandForClipboard(presentation.installMethod, presentation.updateCommand)
                row {
                    button(UnityBundle.message("unity.agents.cli.copy.command")) {
                        CopyPasteManager.getInstance().setContents(StringSelection(command.orEmpty()))
                    }.enabled(command != null).gap(RightGap.SMALL)
                    comment(unityCliUpdateByHandMessage(command))
                }
            }

            UnityCliOffer.BUSY -> {
                row {
                    comment(
                        when (presentation.operation) {
                            UnityCliOperation.UPDATING -> UnityBundle.message("unity.agents.cli.updating")
                            UnityCliOperation.INSTALLING, null -> UnityBundle.message("unity.agents.cli.installing")
                        }
                    )
                }
                row {
                    cell(buildProgressBar(presentation.operationFraction)).align(AlignX.FILL)
                }
            }

            UnityCliOffer.FAILED -> {
                row {
                    button(UnityBundle.message("unity.agents.cli.try.again")) { retry(presentation) }.gap(RightGap.SMALL)
                    icon(AllIcons.General.Error).gap(RightGap.SMALL)
                    comment(unityCliFailureMessage(presentation.failure, presentation.failureExitCode))
                }

                if (ShowLogAction.isSupported()) {
                    row {
                        link(ShowLogAction.getActionName()) { ShowLogAction.showLog() }
                    }
                }
            }
        }
    }

    private fun buildProgressBar(fraction: Double?): JProgressBar {
        val bar = JProgressBar(0, 100)
        bar.preferredSize = JBUI.size(320, bar.preferredSize.height)
        if (fraction == null) {
            bar.isIndeterminate = true
        }
        else {
            bar.value = (fraction * 100).toInt()
        }
        return bar
    }

    private fun trustProject() {
        if (TrustedProjectsDialog.confirmLoadingUntrustedProject(project)) {
            refresh()
        }
    }

    private fun retry(presentation: UnityCliPresentation) {
        logClick(UnityAgentsAction.RETRY)
        val failureId = presentation.failureId
        if (failureId != null) UnityCliStatusProvider.getInstance().clearFailure(failureId)
        if (presentation.failedOperation == UnityCliOperation.UPDATING) startUpdate() else startInstall()
    }

    private fun recheck() {
        UnityCliStatusProvider.getInstance().forgetUpdateInfo()
        refresh()
    }

    private fun startInstall() {
        if (!UnityForAgentsFeature.isEnabled()) return
        val activeScope = scope ?: return
        val modality = ModalityState.current()

        refreshJob?.cancel()
        paintedPage = null
        cliStatus?.component = buildCliStatus(UnityCliPresentation(detected = true, operation = UnityCliOperation.INSTALLING))
        activeScope.launch {
            UnityCliStatusProvider.getInstance().install(project, modality, UnityAgentsSource.SETTINGS)
            withContext(Dispatchers.EDT + modality.asContextElement()) { refresh() }
        }
    }

    private fun startUpdate() {
        if (!UnityForAgentsFeature.isEnabled()) return
        val activeScope = scope ?: return
        val modality = ModalityState.current()
        refreshJob?.cancel()
        paintedPage = null
        cliStatus?.component = buildCliStatus(UnityCliPresentation(detected = true, operation = UnityCliOperation.UPDATING))
        activeScope.launch {
            UnityCliStatusProvider.getInstance().update(project, modality, UnityAgentsSource.SETTINGS)
            withContext(Dispatchers.EDT + modality.asContextElement()) { refresh() }
        }
    }
}

private val BUSY_REFRESH_INTERVAL = 1.seconds
