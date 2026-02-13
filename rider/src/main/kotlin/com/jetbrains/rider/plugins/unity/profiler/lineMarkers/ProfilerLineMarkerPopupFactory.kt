package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.icons.AllIcons
import com.intellij.ide.DataManager
import com.intellij.openapi.actionSystem.ActionPlaces
import com.intellij.openapi.actionSystem.ActionUiKind
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.impl.PresentationFactory
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
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.components.JBLabel
import com.intellij.ui.popup.ActionPopupOptions
import com.intellij.ui.popup.ActionPopupStep
import com.intellij.ui.popup.PopupFactoryImpl
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ModelUnityProfilerSampleInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ParentCalls
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.profiler.actions.ShowUnityProfilerSettingsAction
import com.jetbrains.rider.plugins.unity.profiler.actions.ToggleGutterMarksViewAction
import com.jetbrains.rider.plugins.unity.profiler.actions.ToggleUnityProfilerGutterMarksAction
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.UnityProfilerToolWindowFactory
import com.jetbrains.rider.plugins.unity.profiler.utils.UnityProfilerFormatUtils
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerLineMarkerViewModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import kotlinx.coroutines.CoroutineStart
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import java.awt.BorderLayout
import java.awt.Component
import java.awt.event.MouseEvent
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
        val actionItems = createActionItems(project, markerViewModel, sampleInfo)
        val items = parents + actionItems

        val builder = JBPopupFactory.getInstance()
            .createPopupChooserBuilder(items)
            .setRenderer(RoundedCellRenderer(UnifiedRenderer(ParentCallsRenderer(), parents.size)))
            .setNamerForFiltering { item ->
                when (item) {
                    is ParentCalls -> item.qualifiedName
                    is PopupFactoryImpl.ActionItem -> item.text
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

    private fun createActionItems(project: Project, markerViewModel: UnityProfilerLineMarkerViewModel, sampleInfo: ModelUnityProfilerSampleInfo): List<PopupFactoryImpl.ActionItem> {
        val actionGroup = DefaultActionGroup().apply {
            add(object : DumbAwareAction(UnityUIBundle.message("action.show.in.unity.profiler.toolwindow.text"), null, AllIcons.Actions.MoveTo2) {
                override fun actionPerformed(e: AnActionEvent) {
                    UnityProfilerToolWindowFactory.showAndNavigate(project, sampleInfo.qualifiedName)
                }
            })
            add(ToggleUnityProfilerGutterMarksAction(markerViewModel.isGutterMarksEnabled))
            if(markerViewModel.isGutterMarksEnabled.valueOrDefault(false))
                add(ToggleGutterMarksViewAction(markerViewModel.gutterMarksRenderSettings))
            add(ShowUnityProfilerSettingsAction())
        }
        
        val dataContext = DataManager.getInstance().dataContext
        val presentationFactory = PresentationFactory()
        
        return ActionPopupStep.createActionItems(
            actionGroup,
            dataContext,
            ActionPlaces.UNKNOWN,
            presentationFactory,
            ActionPopupOptions.showDisabled()
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
                is PopupFactoryImpl.ActionItem -> {
                    val dataContext = DataManager.getInstance().getDataContext(gutter)
                    val presentation = element.action.templatePresentation.clone()
                    val event = AnActionEvent.createEvent(element.action, dataContext, presentation, ActionPlaces.UNKNOWN, ActionUiKind.NONE, e)
                    element.action.actionPerformed(event)
                }
            }
        }
    }

    private fun createHeader(sampleInfo: ModelUnityProfilerSampleInfo): JComponent {
        val headerBackground = JBUI.CurrentTheme.Editor.Tooltip.BACKGROUND
        val headerForeground = SimpleTextAttributes.GRAYED_ATTRIBUTES.fgColor

        val panel = JPanel().apply {
            layout = BoxLayout(this, BoxLayout.Y_AXIS)
            this.background = headerBackground
            border = JBUI.Borders.empty(8, 12)

            val cpuText = UnityUIBundle.message("gutter.popup.label.cpu", UnityProfilerFormatUtils.formatMs(sampleInfo.milliseconds).trim(),UnityProfilerFormatUtils.formatPercentage(sampleInfo.framePercentage))
            val allocationsText = UnityUIBundle.message("gutter.popup.label.gc.alloc", UnityProfilerFormatUtils.formatFileSize(sampleInfo.memoryAllocation))
            val statsText = UnityUIBundle.message("gutter.popup.label.stats",
                sampleInfo.callesCount,
                UnityProfilerFormatUtils.formatMs(sampleInfo.stats.min),
                UnityProfilerFormatUtils.formatMs(sampleInfo.stats.max),
                UnityProfilerFormatUtils.formatMs(sampleInfo.stats.avg)
            )

            add(JBLabel(cpuText).apply {
                this.foreground = headerForeground
                this.font = UIUtil.getToolTipFont()
                alignmentX = Component.LEFT_ALIGNMENT
            })
            add(JBLabel(allocationsText).apply {
                this.foreground = headerForeground
                this.font = UIUtil.getToolTipFont()
                alignmentX = Component.LEFT_ALIGNMENT
            })
            add(JBLabel(statsText).apply {
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
                is PopupFactoryImpl.ActionItem -> {
                    val label = JBLabel(value.text, value.getIcon(isSelected), JBLabel.LEFT).apply {
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
                val text = "${UnityProfilerFormatUtils.formatMs(value.duration)} (${UnityProfilerFormatUtils.formatPercentage(value.framePercentage)})"
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
