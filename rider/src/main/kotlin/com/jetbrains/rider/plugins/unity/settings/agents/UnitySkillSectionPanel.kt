package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.icons.AllIcons
import com.intellij.ide.DataManager
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.Separator
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.ui.AnimatedIcon
import com.intellij.ui.components.ActionLink
import com.intellij.ui.components.JBOptionButton
import com.intellij.ui.dsl.builder.AlignX
import com.intellij.ui.dsl.builder.AlignY
import com.intellij.ui.dsl.builder.MAX_LINE_LENGTH_WORD_WRAP
import com.intellij.ui.dsl.builder.Panel
import com.intellij.ui.dsl.builder.RightGap
import com.intellij.ui.dsl.builder.components.DslLabel
import com.intellij.ui.dsl.builder.components.DslLabelType
import com.intellij.ui.dsl.builder.panel
import com.intellij.util.ui.UIUtil
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.Nls
import java.awt.event.ActionEvent
import javax.swing.AbstractAction
import javax.swing.JComponent

internal class UnitySkillSectionPanel(
    private val install: (UnitySkillTarget) -> Unit,
    private val retry: (UnitySkillPresentation) -> Unit,
    private val openSkillsSettings: (JComponent) -> Unit,
    private val browseSkills: (JComponent) -> Unit,
) {
    fun build(presentation: UnitySkillPresentation): JComponent = panel {
        when (presentation.offer) {
            UnitySkillOffer.GATED -> {
                if (presentation.gate == UnityMcpSectionState.NO_CLI || presentation.gate == UnityMcpSectionState.READY) {
                    row {
                        cell(createSplitButton(enabled = false))
                    }
                    browseRow()
                }
                unitySkillGateMessage(presentation.gate, presentation.packageInstalled, presentation.blockingEditor)?.let { message ->
                    row { comment(message) }
                }
            }

            UnitySkillOffer.CHECKING -> row {
                icon(AnimatedIcon.Default.INSTANCE).gap(RightGap.SMALL)
                comment(UnityBundle.message("unity.agents.skills.checking"))
            }

            UnitySkillOffer.INSTALL -> {
                row {
                    cell(createSplitButton(enabled = true, unitySkillOpenTargets(presentation.installs.orEmpty())))
                }
                browseRow()
                row {
                    comment(UnityBundle.message("unity.agents.skills.hint"))
                }
            }

            UnitySkillOffer.INSTALLED -> {
                row {
                    icon(AllIcons.General.InspectionsOK).align(AlignY.TOP).gap(RightGap.SMALL)
                    label(UnityBundle.message("unity.agents.skills.installed"))
                        .applyToComponent { foreground = UIUtil.getLabelSuccessForeground() }
                    val places = unitySkillPlacesMessage(presentation.installs.orEmpty())
                    if (places != null) {
                        cell(LinkAfterText(placesText(places), installForLink(presentation))).align(AlignX.FILL)
                    }
                }
                browseRow()
            }

            UnitySkillOffer.BUSY -> row {
                icon(AnimatedIcon.Default.INSTANCE).gap(RightGap.SMALL)
                comment(UnityBundle.message("unity.agents.skills.installing"))
            }

            UnitySkillOffer.FAILED -> failureRows(
                UnityBundle.message("unity.agents.skills.try.again"),
                unitySkillFailureMessage(presentation.failure ?: UnitySkillFailure.NOT_CONFIRMED),
            ) { retry(presentation) }
        }
    }

    private fun placesText(@Nls places: String): DslLabel = DslLabel(DslLabelType.LABEL).apply {
        maxLineLength = MAX_LINE_LENGTH_WORD_WRAP
        text = places
        foreground = UIUtil.getContextHelpForeground()
    }

    private fun installForLink(presentation: UnitySkillPresentation): ActionLink? {
        if (presentation.gate != UnityMcpSectionState.READY || !presentation.packageInstalled) return null
        val openTargets = unitySkillOpenTargets(presentation.installs.orEmpty())
        if (openTargets.isEmpty()) return null
        return ActionLink(UnityBundle.message("unity.agents.skills.install.for")) { event ->
            val link = event.source as? JComponent ?: return@ActionLink
            val group = DefaultActionGroup(dropDownActions(link, openTargets))
            JBPopupFactory.getInstance()
                .createActionGroupPopup(
                    null,
                    group,
                    DataManager.getInstance().getDataContext(link),
                    JBPopupFactory.ActionSelectionAid.SPEEDSEARCH,
                    false,
                )
                .showUnderneathOf(link)
        }
    }

    private fun Panel.browseRow() {
        row {
            link(UnityBundle.message("unity.agents.skills.browse")) { event ->
                val link = event.source as? JComponent ?: return@link
                browseSkills(link)
            }
        }
    }

    private fun createSplitButton(
        enabled: Boolean,
        targets: List<UnitySkillTarget> = emptyList(),
    ): JBOptionButton {
        val action = object : AbstractAction(UnityBundle.message("unity.agents.skills.install")) {
            override fun actionPerformed(e: ActionEvent?) {
                install(UnitySkillTarget.ALL)
            }
        }
        action.isEnabled = enabled
        val button = JBOptionButton(action, null)
        button.isEnabled = enabled
        if (enabled) {
            button.setOptions(dropDownActions(button, targets))
        }
        return button
    }

    private fun dropDownActions(
        component: JComponent,
        targets: List<UnitySkillTarget>,
    ): List<AnAction> {
        val actions = mutableListOf<AnAction>()
        for (target in targets) {
            val text = when (target) {
                UnitySkillTarget.CODEX -> UnityBundle.message("unity.agents.skills.install.for.codex")
                UnitySkillTarget.CLAUDE_CODE -> UnityBundle.message("unity.agents.skills.install.for.claude.code")
                UnitySkillTarget.COPILOT -> UnityBundle.message("unity.agents.skills.install.for.copilot")
                UnitySkillTarget.JUNIE -> UnityBundle.message("unity.agents.skills.install.for.junie")
                UnitySkillTarget.ALL -> null
            } ?: continue
            actions.add(DumbAwareAction.create(text) { install(target) })
        }
        if (targets.isNotEmpty()) {
            actions.add(Separator.getInstance())
        }
        actions.add(
            DumbAwareAction.create(UnityBundle.message("unity.agents.skills.open.settings")) {
                openSkillsSettings(component)
            },
        )
        return actions
    }
}
