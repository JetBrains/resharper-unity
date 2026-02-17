package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.Separator
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ex.ToolWindowEx
import com.intellij.ui.components.JBScrollPane
import com.intellij.ui.dsl.builder.AlignX
import com.intellij.ui.dsl.builder.AlignY
import com.intellij.ui.dsl.builder.panel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerUsagesDaemon
import com.jetbrains.rider.plugins.unity.profiler.actions.ShowUnityProfilerSettingsAction
import com.jetbrains.rider.plugins.unity.profiler.actions.ToggleUnityProfilerAutoFetchAction
import com.jetbrains.rider.plugins.unity.profiler.actions.ToggleUnityProfilerGutterMarksAction
import com.jetbrains.rider.plugins.unity.profiler.actions.ToggleUnityProfilerIntegrationAction
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import java.awt.BorderLayout
import java.awt.CardLayout
import javax.swing.JComponent
import javax.swing.JPanel

class UnityProfilerToolContent(
    project: Project,
    projectLifetime: Lifetime,
    toolWindow: ToolWindow
) : JPanel(CardLayout()) {

    private val daemon = project.service<UnityProfilerUsagesDaemon>()
    private val lifetime = Lifetime.intersect(projectLifetime, daemon.lifetime)
    private val chart = UnityProfilerChart(daemon.chartViewModel, daemon.snapshotModel, project, lifetime)
    private val treeTable = UnityProfilerTreeTable(daemon.treeViewModel, project, lifetime)
    private val filterField = UnityProfilerFilterField(daemon.treeViewModel, lifetime)
    private val settings get() = daemon.settingsModel

    companion object {
        private const val CONTENT_CARD = "CONTENT"
        private const val NO_DATA_CARD = "NO_DATA"
        private const val DISABLED_CARD = "DISABLED"

        private fun createNoDataPanel(): JComponent = panel {
            row {
                panel {
                    row {
                        icon(AllIcons.Nodes.Package).align(AlignY.CENTER)
                        panel {
                            row {
                                label(UnityUIBundle.message("unity.profiler.toolwindow.no.data.title")).bold()
                            }
                            row {
                                label(UnityUIBundle.message("unity.profiler.toolwindow.no.data.description"))
                            }
                        }
                    }
                }.align(AlignX.CENTER).align(AlignY.CENTER)
            }.resizableRow()
        }

        private fun createDisabledPanel(onEnable: () -> Unit): JComponent = panel {
            row {
                panel {
                    row {
                        icon(AllIcons.Nodes.Package).align(AlignY.CENTER)
                        panel {
                            row {
                                label(UnityUIBundle.message("unity.profiler.toolwindow.integration.disabled.title")).bold()
                            }
                            row {
                                label(UnityUIBundle.message("unity.profiler.toolwindow.integration.disabled.description"))
                            }
                            row {
                                link(UnityUIBundle.message("unity.profiler.toolwindow.integration.disabled.enable")) {
                                    onEnable()
                                }
                            }
                        }
                    }
                }.align(AlignX.CENTER).align(AlignY.CENTER)
            }.resizableRow()
        }
    }

    init {
        if (toolWindow is ToolWindowEx) {
            toolWindow.setAdditionalGearActions(DefaultActionGroup().apply {
                add(ToggleUnityProfilerIntegrationAction(settings))
                add(ToggleUnityProfilerAutoFetchAction(settings))
                add(ToggleUnityProfilerGutterMarksAction(settings.isGutterMarksEnabled))
                add(Separator.getInstance())
                add(ShowUnityProfilerSettingsAction())
            })
        }

        val contentPanel = JPanel(BorderLayout()).apply {
            val treeTablePanel = JPanel(BorderLayout()).apply {
                add(filterField, BorderLayout.NORTH)
                add(JBScrollPane(treeTable), BorderLayout.CENTER)
            }

            add(chart.component, BorderLayout.NORTH)
            add(treeTablePanel, BorderLayout.CENTER)
        }

        add(contentPanel, CONTENT_CARD)
        add(createNoDataPanel(), NO_DATA_CARD)
        add(createDisabledPanel { settings.isIntegrationEnabled.set(true) }, DISABLED_CARD)

        val cardLayout = layout as CardLayout
        fun updateCard() {
            val profilerRecordInfo = daemon.treeViewModel.currentProfilerRecordInfo.value
            val integrationEnabledValue = settings.isIntegrationEnabled.valueOrNull
            
            if(integrationEnabledValue == null || profilerRecordInfo == null)
                return cardLayout.show(this, NO_DATA_CARD)
            
            if (!integrationEnabledValue) 
                cardLayout.show(this, DISABLED_CARD) 
            else 
                cardLayout.show(this, CONTENT_CARD)
        }

        settings.isIntegrationEnabled.advise(lifetime) { updateCard() }
        daemon.treeViewModel.currentProfilerRecordInfo.advise(lifetime) {
            updateCard()
        }
    }
}
