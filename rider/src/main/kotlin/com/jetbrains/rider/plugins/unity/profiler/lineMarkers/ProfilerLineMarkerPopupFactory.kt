package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.icons.AllIcons
import com.intellij.ide.DataManager
import com.intellij.openapi.actionSystem.ActionPlaces
import com.intellij.openapi.actionSystem.ActionUiKind
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.application.EDT
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.diagnostic.runAndLogException
import com.intellij.openapi.editor.ex.EditorGutterComponentEx
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.popup.JBPopup
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.openapi.ui.popup.PopupChooserBuilder
import com.intellij.openapi.ui.popup.util.RoundedCellRenderer
import com.intellij.ui.SeparatorComponent
import com.intellij.ui.SimpleListCellRenderer
import com.intellij.ui.components.JBLabel
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ModelUnityProfilerSampleInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ParentCalls
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.profiler.actions.ShowUnityProfilerSettingsAction
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.UnityProfilerToolWindowFactory
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerLineMarkerViewModel
import kotlinx.coroutines.CoroutineStart
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import java.awt.BorderLayout
import java.awt.Component
import java.awt.event.MouseEvent
import java.util.*
import javax.swing.*

object ProfilerLineMarkerPopupFactory {
    private val logger = Logger.getInstance(ProfilerLineMarkerPopupFactory::class.java)

    fun create(
        project: Project,
        sampleInfo: ModelUnityProfilerSampleInfo,
        markerViewModel: UnityProfilerLineMarkerViewModel,
        gutter: EditorGutterComponentEx,
        e: MouseEvent
    ): JBPopup {
        val parents = sampleInfo.parents ?: emptyList()
        val actions = createActions(project, markerViewModel, sampleInfo)
        val items = parents + actions

        val builder = JBPopupFactory.getInstance()
            .createPopupChooserBuilder(items)
            .setRenderer(RoundedCellRenderer(UnifiedRenderer(ParentCallsRenderer(), parents.size)))
            .setNamerForFiltering { item ->
                when (item) {
                    is ParentCalls -> item.qualifiedName
                    is AnAction -> item.templatePresentation.text ?: ""
                    else -> ""
                }
            }
            .setItemChosenCallback { element ->
                handleItemSelection(element, project, markerViewModel, gutter, e)
            }

        val header = createHeader(sampleInfo)
        (builder as? PopupChooserBuilder<Any>)?.setNorthComponent(header)

        return builder.createPopup()
    }

    private fun createActions(project: Project, markerViewModel: UnityProfilerLineMarkerViewModel, sampleInfo: ModelUnityProfilerSampleInfo): List<AnAction> {
        val gutterMarkRenderSettings = markerViewModel.gutterMarksRenderSettings.valueOrDefault(ProfilerGutterMarkRenderSettings.Default)
        return listOfNotNull(
            when (gutterMarkRenderSettings) {
                ProfilerGutterMarkRenderSettings.Default -> MinimizeUnityProfilerGutterMarksWithIconAction()
                ProfilerGutterMarkRenderSettings.Minimized -> MaximizeUnityProfilerGutterMarksWithIconAction()
                else -> null
            },
            object : DumbAwareAction("Show in Unity Profiler Toolwindow", null, AllIcons.Actions.MoveTo2) {
                override fun actionPerformed(e: AnActionEvent) {
                    UnityProfilerToolWindowFactory.showAndNavigate(project, sampleInfo.qualifiedName)
                }
            },
            HideUnityProfilerGutterMarksWithIconAction(),
            ShowUnityProfilerSettingsAction()
        )
    }

    private fun handleItemSelection(
        element: Any?,
        project: Project,
        markerViewModel: UnityProfilerLineMarkerViewModel,
        gutter: EditorGutterComponentEx,
        e: MouseEvent
    ) {
        if (element == null) return
        logger.runAndLogException {
            when (element) {
                is ParentCalls -> {
                    UnityProjectLifetimeService.getScope(project).launch(Dispatchers.EDT, CoroutineStart.UNDISPATCHED) {
                        logger.runAndLogException {
                            if (element.realParentQualifiedName != null)
                                markerViewModel.navigateByQualifiedName(element.realParentQualifiedName)
                        }
                    }
                }
                is AnAction -> {
                    val dataContext = DataManager.getInstance().getDataContext(gutter)
                    val event = AnActionEvent.createEvent(element, dataContext, element.templatePresentation.clone(), ActionPlaces.UNKNOWN, ActionUiKind.NONE, e)
                    element.actionPerformed(event)
                }
            }
        }
    }

    private fun createHeader(sampleInfo: ModelUnityProfilerSampleInfo): JComponent {
        val headerBackground = JBUI.CurrentTheme.Editor.Tooltip.BACKGROUND
        val headerForeground = JBUI.CurrentTheme.Editor.Tooltip.FOREGROUND

        val panel = JPanel().apply {
            layout = BoxLayout(this, BoxLayout.Y_AXIS)
            this.background = headerBackground
            border = JBUI.Borders.empty(8, 12)

            val cpuText = "CPU: ${ProfilerFormattingUtils.formatFixedWidthDuration(sampleInfo.milliseconds).trim()} (${String.format(Locale.US, "%.1f%%", sampleInfo.framePercentage * 100.0)})"
            val statsText = "Stats: ${String.format(Locale.US, "%d calls (Min: %.2f ms, Max: %.2f ms, Avg: %.2f ms)", sampleInfo.callesCount, sampleInfo.stats.min, sampleInfo.stats.max, sampleInfo.stats.avg)}"
            val allocationsText = "Allocations: ${ProfilerFormattingUtils.formatMemory(sampleInfo.memoryAllocation)}"

            add(JBLabel(cpuText).apply {
                this.foreground = headerForeground
                this.font = UIUtil.getToolTipFont()
                alignmentX = Component.LEFT_ALIGNMENT
            })
            add(JBLabel(statsText).apply {
                this.foreground = headerForeground
                this.font = UIUtil.getToolTipFont()
                alignmentX = Component.LEFT_ALIGNMENT
            })
            add(JBLabel(allocationsText).apply {
                this.foreground = headerForeground
                this.font = UIUtil.getToolTipFont()
                alignmentX = Component.LEFT_ALIGNMENT
            })
        }

        return JPanel(BorderLayout()).apply {
            this.background = headerBackground
            add(panel, BorderLayout.CENTER)
            add(SeparatorComponent(0, 0, JBUI.CurrentTheme.Popup.separatorColor(), null).apply {
                background = headerBackground
            }, BorderLayout.SOUTH)
        }
    }

    private class UnifiedRenderer(
        private val parentCallsRenderer: ParentCallsRenderer,
        private val actionsStartIndex: Int
    ) : ListCellRenderer<Any> {
        override fun getListCellRendererComponent(list: JList<out Any>, value: Any, index: Int, isSelected: Boolean, cellHasFocus: Boolean): Component {
            val component = when (value) {
                is ParentCalls -> {
                    @Suppress("UNCHECKED_CAST")
                    parentCallsRenderer.getListCellRendererComponent(list as JList<out ParentCalls>, value, index, isSelected, cellHasFocus)
                }
                is AnAction -> {
                    val presentation = value.templatePresentation
                    val label = JBLabel(presentation.text ?: "", presentation.icon, JBLabel.LEFT).apply {
                        iconTextGap = JBUI.scale(8)
                        border = JBUI.Borders.empty(4, 4)
                    }
                    JPanel(BorderLayout()).apply {
                        add(label, BorderLayout.WEST)
                        UIUtil.setBackgroundRecursively(this, if (isSelected) list.selectionBackground else list.background)
                        UIUtil.setForegroundRecursively(this, if (isSelected) list.selectionForeground else list.foreground)
                    }
                }
                else -> JBLabel("???")
            }

            if (index == actionsStartIndex && index > 0) {
                return JPanel(BorderLayout()).apply {
                    background = list.background
                    add(SeparatorComponent(JBUI.scale(2), JBUI.scale(2), JBUI.CurrentTheme.Popup.separatorColor(), null), BorderLayout.NORTH)
                    add(component, BorderLayout.CENTER)
                }
            }
            return component
        }
    }

    private class ParentCallsRenderer : ListCellRenderer<ParentCalls> {

        private val panel = JPanel(BorderLayout(JBUI.scale(4), 0)).apply {
            border = JBUI.Borders.empty(0, JBUI.scale(4))
        }

        private val mainRenderer =
            SimpleListCellRenderer.create<ParentCalls> { label, value, _ ->
                label.text = value.qualifiedName
            }

        override fun getListCellRendererComponent(
            list: JList<out ParentCalls>,
            value: ParentCalls,
            index: Int,
            isSelected: Boolean,
            cellHasFocus: Boolean
        ): Component {
            val left = mainRenderer.getListCellRendererComponent(list, value, index, isSelected, cellHasFocus)

            // Right side: duration and percentage, formatted like label used in gutter
            val right = JPanel(BorderLayout(JBUI.scale(2), 0)).apply {
                val text = ProfilerFormattingUtils.formatLabel("", value.duration, value.framePercentage)
                // text comes as " 99.99ms (9.9%)" after trimming empty name
                add(JBLabel(text), BorderLayout.EAST)
            }

            panel.removeAll()
            panel.add(left, BorderLayout.WEST)
            panel.add(right, BorderLayout.EAST)

            UIUtil.setBackgroundRecursively(panel, left.background)
            UIUtil.setForegroundRecursively(panel, left.foreground)
            return panel
        }
    }
}
