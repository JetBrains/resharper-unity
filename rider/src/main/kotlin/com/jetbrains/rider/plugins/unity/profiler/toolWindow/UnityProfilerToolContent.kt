package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.ide.ui.laf.darcula.ui.DarculaButtonUI
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.Separator
import com.intellij.openapi.components.service
import com.intellij.openapi.help.HelpManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ex.ToolWindowEx
import com.intellij.ui.components.ActionLink
import com.intellij.ui.components.JBScrollPane
import com.intellij.ui.dsl.builder.AlignX
import com.intellij.ui.dsl.builder.AlignY
import com.intellij.ui.dsl.builder.panel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerUsagesDaemon
import com.jetbrains.rider.plugins.unity.profiler.actions.ShowUnityProfilerSettingsAction
import com.jetbrains.rider.plugins.unity.profiler.actions.ToggleUnityProfilerAutoFetchAction
import com.jetbrains.rider.plugins.unity.profiler.actions.ToggleUnityProfilerGutterMarksAction
import com.jetbrains.rider.plugins.unity.profiler.actions.ToggleUnityProfilerIntegrationAction
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import icons.UnityIcons
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
        private const val UNITY_NOT_CONNECTED_CARD = "UNITY_NOT_CONNECTED"

        private val unityIcon = UnityIcons.Common.UnityToolWindow

        private fun createNoDataPanel(): JComponent {
            return panel {
                row {
                    panel {
                        row {
                            icon(unityIcon).align(AlignY.TOP)
                            panel {
                                row {
                                    label(UnityUIBundle.message("unity.profiler.toolwindow.no.data.title")).bold()
                                }
                                row {
                                    comment(UnityUIBundle.message("unity.profiler.toolwindow.no.data.description"))
                                }
                                row {
                                    cell(createLink())
                                }
                            }
                        }
                    }.align(AlignX.CENTER).align(AlignY.CENTER)
                }.resizableRow()
            }
        }

        private fun createDisabledPanel(onEnable: () -> Unit): JComponent = panel {
            row {
                panel {
                    row {
                        icon(unityIcon).align(AlignY.TOP)
                        panel {
                            row {
                                label(UnityUIBundle.message("unity.profiler.toolwindow.integration.disabled.title")).bold()
                            }
                            row {
                                comment(UnityUIBundle.message("unity.profiler.toolwindow.integration.disabled.description"))
                            }
                            row {
                                button(UnityUIBundle.message("unity.profiler.toolwindow.integration.disabled.enable")) {
                                    onEnable()
                                }.component.apply {
                                    putClientProperty(DarculaButtonUI.DEFAULT_STYLE_KEY, true)
                                }
                                cell(createLink())
                            }
                        }
                    }
                }.align(AlignX.CENTER).align(AlignY.CENTER)
            }.resizableRow()
        }

        private fun createLink(): ActionLink {
            return ActionLink(UnityUIBundle.message("unity.profiler.toolwindow.integration.disabled.learn.more")) {
                HelpManager.getInstance().invokeHelp("Settings_Unity_Engine_Profiler_Integration_Learn_More_ToolWindow")
            }
        }

        private fun createUnityNotConnectedPanel(): JComponent {
            return panel {
                row {
                    panel {
                        row {
                            icon(unityIcon).align(AlignY.TOP)
                            panel {
                                row {
                                    label(UnityUIBundle.message("unity.profiler.toolwindow.not.connected.title")).bold()
                                }
                                row {
                                    comment(UnityUIBundle.message("unity.profiler.toolwindow.not.connected.description"))
                                }
                                row {
                                    cell(createLink())
                                }
                            }
                        }
                    }.align(AlignX.CENTER).align(AlignY.CENTER)
                }.resizableRow()
            }
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
        add(createUnityNotConnectedPanel(), UNITY_NOT_CONNECTED_CARD)

        val cardLayout = layout as CardLayout
        fun updateCard() {
            val profilerRecordInfo = daemon.treeViewModel.currentProfilerRecordInfo.value
            val integrationEnabledValue = settings.isIntegrationEnabled.valueOrDefault(false)
            val unityEditorConnected = daemon.unityEditorConnected.valueOrDefault(false)

            if (!integrationEnabledValue)
                cardLayout.show(this, DISABLED_CARD)
            else if (!unityEditorConnected)
                cardLayout.show(this, UNITY_NOT_CONNECTED_CARD)
            else if(profilerRecordInfo == null)
                return cardLayout.show(this, NO_DATA_CARD)
            else 
                cardLayout.show(this, CONTENT_CARD)
        }

        settings.isIntegrationEnabled.advise(lifetime) { updateCard() }
        daemon.unityEditorConnected.advise(lifetime) { updateCard() }
        daemon.treeViewModel.currentProfilerRecordInfo.advise(lifetime) {
            updateCard()
        }
    }
}
