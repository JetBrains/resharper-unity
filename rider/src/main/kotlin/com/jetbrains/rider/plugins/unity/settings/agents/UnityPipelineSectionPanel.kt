package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.icons.AllIcons
import com.intellij.ide.actions.ShowLogAction
import com.intellij.ui.AnimatedIcon
import com.intellij.ui.dsl.builder.MAX_LINE_LENGTH_NO_WRAP
import com.intellij.ui.dsl.builder.Panel
import com.intellij.ui.dsl.builder.RightGap
import com.intellij.ui.dsl.builder.panel
import com.intellij.util.ui.UIUtil
import com.jetbrains.rider.plugins.unity.UnityBundle
import javax.swing.JComponent

internal class UnityPipelineSectionPanel(
    private val install: () -> Unit,
    private val upgrade: () -> Unit,
    private val retry: (UnityPipelinePresentation) -> Unit,
) {
    fun build(presentation: UnityPipelinePresentation): JComponent = panel {
        when (presentation.offer) {
            UnityPipelineOffer.GATED -> {
                if (presentation.gate == UnityMcpSectionState.NO_CLI) {
                    row {
                        button(UnityBundle.message("unity.agents.pipeline.install")) {}.enabled(false)
                    }
                }
                unityPipelineGateMessage(presentation.gate)?.let { message ->
                    row { comment(message) }
                }
            }

            UnityPipelineOffer.CHECKING -> row {
                icon(AnimatedIcon.Default.INSTANCE).gap(RightGap.SMALL)
                comment(UnityBundle.message("unity.agents.pipeline.checking"))
            }

            UnityPipelineOffer.UNREADABLE -> row {
                icon(AllIcons.General.Error).gap(RightGap.SMALL)
                comment(UnityBundle.message("unity.agents.pipeline.unreadable"))
            }

            UnityPipelineOffer.INSTALL -> {
                row {
                    button(UnityBundle.message("unity.agents.pipeline.install")) { install() }
                }
                row {
                    comment(unityPipelineInstallHint(presentation.latestVersion))
                }
            }

            UnityPipelineOffer.INSTALLED -> installedRow(presentation)

            UnityPipelineOffer.UPGRADE -> {
                installedRow(presentation)
                row {
                    button(UnityBundle.message("unity.agents.pipeline.update")) { upgrade() }.gap(RightGap.SMALL)
                    comment(unityPipelineNewVersionMessage(presentation.latestVersion.orEmpty()))
                }
            }

            UnityPipelineOffer.BUSY -> row {
                icon(AnimatedIcon.Default.INSTANCE).gap(RightGap.SMALL)
                comment(unityPipelineBusyMessage(presentation.operation ?: UnityPipelineOperation.INSTALLING))
            }

            UnityPipelineOffer.FAILED -> {
                row {
                    button(UnityBundle.message("unity.agents.pipeline.try.again")) { retry(presentation) }.gap(RightGap.SMALL)
                    icon(AllIcons.General.Error).gap(RightGap.SMALL)
                    comment(
                        unityPipelineFailureMessage(
                            presentation.failure ?: UnityPipelineFailure.NOT_CONFIRMED,
                            presentation.failureExitCode,
                        )
                    )
                }

                if (ShowLogAction.isSupported()) {
                    row {
                        link(ShowLogAction.getActionName()) { ShowLogAction.showLog() }
                    }
                }
            }
        }
    }

    private fun Panel.installedRow(presentation: UnityPipelinePresentation) {
        row {
            icon(AllIcons.General.InspectionsOK).gap(RightGap.SMALL)
            label(UnityBundle.message("unity.agents.pipeline.installed"))
                .applyToComponent { foreground = UIUtil.getLabelSuccessForeground() }
                .gap(RightGap.SMALL)
            text(unityPipelineVersionMessage(presentation.manifest?.version.orEmpty()), MAX_LINE_LENGTH_NO_WRAP)
        }
    }
}
