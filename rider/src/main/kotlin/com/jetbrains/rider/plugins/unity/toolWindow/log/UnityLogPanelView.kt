package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.execution.filters.Filter
import com.intellij.execution.filters.TextConsoleBuilderFactory
import com.intellij.execution.impl.ConsoleViewImpl
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.editor.actions.ToggleUseSoftWrapsToolbarAction
import com.intellij.openapi.extensions.Extensions
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.wm.IdeFocusManager
import com.intellij.ui.DocumentAdapter
import com.intellij.ui.DoubleClickListener
import com.intellij.ui.JBSplitter
import com.intellij.ui.PopupHandler
import com.intellij.ui.components.JBScrollPane
import com.intellij.unscramble.AnalyzeStacktraceUtil
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEvent
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType
import com.jetbrains.rider.settings.RiderUnitySettings
import com.jetbrains.rider.ui.RiderSimpleToolWindowWithTwoToolbarsPanel
import com.jetbrains.rider.ui.RiderUI
import com.jetbrains.rider.unitTesting.panels.RiderUnitTestSessionPanel
import com.jetbrains.rider.util.idea.application
import net.miginfocom.swing.MigLayout
import java.awt.BorderLayout
import java.awt.Component
import java.awt.event.KeyAdapter
import java.awt.event.KeyEvent
import java.awt.event.MouseEvent
import java.text.SimpleDateFormat
import java.util.*
import javax.swing.Icon
import javax.swing.JMenuItem
import javax.swing.JPanel
import javax.swing.JPopupMenu
import javax.swing.event.DocumentEvent

class UnityLogPanelView(project: Project, private val logModel: UnityLogPanelModel, unityHost: UnityHost) {
    private val console = TextConsoleBuilderFactory.getInstance()
        .createBuilder(project)
        .filters(*Extensions.getExtensions<Filter>(AnalyzeStacktraceUtil.EP_NAME.name, project))
        .console as ConsoleViewImpl

    private val eventList = UnityLogPanelEventList().apply {
        addListSelectionListener {
            if (selectedValue != null && logModel.selectedItem != selectedValue) {
                logModel.selectedItem = selectedValue

                console.clear()
                if (selectedIndex >= 0) {
                    val date = getDateFromTicks(selectedValue.time)
                    var format = SimpleDateFormat("[HH:mm:ss:SSS] ")
                    format.timeZone = TimeZone.getDefault()
                    console.print(format.format(date), ConsoleViewContentType.NORMAL_OUTPUT)
                    console.print(selectedValue.message + "\n", ConsoleViewContentType.NORMAL_OUTPUT)
                    console.print(selectedValue.stackTrace, ConsoleViewContentType.NORMAL_OUTPUT)
                    console.scrollTo(0)
                }
            }
        }

        val eventList1 = this
        addKeyListener(object : KeyAdapter() {
            override fun keyPressed(e: KeyEvent?) {
                if (e?.keyCode == KeyEvent.VK_ENTER) {
                    e.consume()
                    getNavigatableForSelected(eventList1, project)?.navigate(true)
                }
            }
        })

        object : DoubleClickListener() {
            override fun onDoubleClick(event: MouseEvent?): Boolean {
                getNavigatableForSelected(eventList1, project)?.navigate(true)
                return true
            }
        }.installOn(this)

        var prevVal: Boolean? = null

        unityHost.play.advise(logModel.lifetime) {
            if (it && prevVal == false) {
                logModel.events.clear()
            }
            prevVal = it
        }
    }

    private fun getDateFromTicks(ticks: Long): Date {
        val ticksAtEpoch = 621355968000000000L
        val ticksPerMilisecond = 10000
        val date = Date((ticks - ticksAtEpoch) / ticksPerMilisecond)
        return date
    }

    val mainSplitterOrientation = RiderUnitySettings.BooleanViewProperty("mainSplitterOrientation")

    private val mainSplitterToggleAction = object : DumbAwareAction("Toggle Output Position", "Toggle Output pane position (right/bottom)", AllIcons.Actions.SplitVertically) {
        override fun actionPerformed(e: AnActionEvent) {
            mainSplitterOrientation.invert()
            update(e)
        }

        override fun update(e: AnActionEvent) {
            e.presentation.icon = getMainSplitterIcon()
        }
    }

    val searchTextField = LogSmartSearchField().apply {
        focusGained = {
            eventList.clearSelection()
            logModel.selectedItem = null
        }
        goToList = {
            if (eventList.model.size>0) {
                eventList.selectedIndex = 0
                IdeFocusManager.getInstance(project).requestFocus(eventList, false)
                true
            } else
                false
        }

        addDocumentListener(object : DocumentAdapter() {
            override fun textChanged(e: DocumentEvent) {
                application.invokeLater {
                    logModel.textFilter.setPattern(text)
                }
            }
        })
    }

    private val listPanel = RiderUI.boxPanel {
        add(JBScrollPane(eventList))
        add(searchTextField, "growx, pushx")
    }

    private val mainSplitter = JBSplitter().apply {
        proportion = 1f / 2
        firstComponent = listPanel
        secondComponent = RiderUI.borderPanel {
            add(console.component, BorderLayout.CENTER)
            console.editor.settings.isCaretRowShown = true
            console.clear()
            console.allowHeavyFilters()
        }
        orientation = mainSplitterOrientation.value
        divider.addMouseListener(object : PopupHandler() {
            override fun invokePopup(comp: Component?, x: Int, y: Int) {
                JPopupMenu().apply {
                    add(JMenuItem("Toggle Output Position", getMainSplitterIcon(true)).apply {
                        addActionListener({ mainSplitterOrientation.invert() })
                    })
                }.show(comp, x, y)
            }
        })
    }

    private val leftToolbar = UnityLogPanelToolbarBuilder.createLeftToolbar(logModel, mainSplitterToggleAction, console.createConsoleActions()
        .filter { it is ToggleUseSoftWrapsToolbarAction }.toList())

    private val topToolbar = UnityLogPanelToolbarBuilder.createTopToolbar()

    fun getMainSplitterIcon(invert: Boolean = false): Icon? = when (mainSplitterOrientation.value xor invert) {
        true -> RiderUnitTestSessionPanel.splitBottomIcon
        false -> RiderUnitTestSessionPanel.splitRightIcon
    }

    val panel = RiderSimpleToolWindowWithTwoToolbarsPanel(leftToolbar, topToolbar, mainSplitter)

    private fun addToList(newEvent: RdLogEvent) {
        if (logModel.mergeSimilarItems.value)
        {
            var existing = eventList.riderModel.elements().toList()
                .filter { it.message == newEvent.message && it.stackTrace==newEvent.stackTrace &&
                    it.mode == newEvent.mode && it.type ==newEvent.type}.singleOrNull()
            if (existing == null)
                eventList.riderModel.addElement(LogPanelItem(newEvent.time, newEvent.type, newEvent.mode,newEvent.message, newEvent.stackTrace,1))
            else
            {
                var index = eventList.riderModel.indexOf(existing)
                eventList.riderModel.setElementAt(LogPanelItem(existing.time, existing.type, existing.mode, existing.message, existing.stackTrace, existing.count+1), index)
            }
        }
        else
            eventList.riderModel.addElement(LogPanelItem(newEvent.time, newEvent.type, newEvent.mode,newEvent.message, newEvent.stackTrace,1))
        // on big amount of logs it causes frontend hangs
//        if (logModel.selectedItem == null) {
//            eventList.ensureIndexIsVisible(eventList.itemsCount - 1)
//        }
        // since we do not follow new items which appear, it makes sence to auto-select first one. RIDER-19937
        if (eventList.itemsCount == 1)
            eventList.selectedIndex = 0
    }

    // TODO: optimize
    private fun refreshList(newEvents: List<LogPanelItem>) {
        eventList.riderModel.clear()
        for (event in newEvents) {
            eventList.riderModel.addElement(event)
        }

        if (logModel.selectedItem != null) {
            eventList.setSelectedValue(logModel.selectedItem, true)
        }
    }

    init {
        Disposer.register(project, console)

        mainSplitterOrientation.advise(logModel.lifetime) { value ->
            mainSplitter.orientation = value
            mainSplitter.updateUI()
        }

        logModel.onAdded.advise(logModel.lifetime) { addToList(it) }
        logModel.onChanged.advise(logModel.lifetime) {
            data class LogItem(
                val type: RdLogEventType,
                val mode: RdLogEventMode,
                val message: String,
                val stackTrace: String)

            if (logModel.mergeSimilarItems.value)
            {
                var list = it
                    .groupBy() { LogItem(it.type, it.mode, it.message, it.stackTrace) }
                    .mapValues { LogPanelItem(it.value.first().time, it.key.type, it.key.mode, it.key.message, it.key.stackTrace, it.value.sumBy { 1 }) }
                    .values.toList()
                refreshList(list)
            }
            else
            {
                var list = it.map { LogPanelItem(it.time, it.type, it.mode, it.message, it.stackTrace,1) }
                refreshList(list)
            }
        }

        logModel.onCleared.advise(logModel.lifetime) { console.clear() }
        logModel.fire()
    }
}