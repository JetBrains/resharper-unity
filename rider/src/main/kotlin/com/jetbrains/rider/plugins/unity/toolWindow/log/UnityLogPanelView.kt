package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.execution.filters.TextConsoleBuilderFactory
import com.intellij.execution.impl.ConsoleViewImpl
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.Separator
import com.intellij.openapi.editor.actions.ToggleUseSoftWrapsToolbarAction
import com.intellij.openapi.editor.markup.TextAttributes
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.wm.IdeFocusManager
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ex.ToolWindowEx
import com.intellij.ui.DocumentAdapter
import com.intellij.ui.DoubleClickListener
import com.intellij.ui.JBSplitter
import com.intellij.ui.PopupHandler
import com.intellij.ui.components.JBScrollPane
import com.intellij.unscramble.AnalyzeStacktraceUtil
import com.intellij.util.application
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.actions.RiderUnityOpenEditorLogAction
import com.jetbrains.rider.plugins.unity.actions.RiderUnityOpenPlayerLogAction
import com.jetbrains.rider.plugins.unity.actions.UnityPluginShowSettingsAction
import com.jetbrains.rider.plugins.unity.model.LogEventMode
import com.jetbrains.rider.plugins.unity.model.LogEventType
import com.jetbrains.rider.plugins.unity.settings.RiderUnitySettings
import com.jetbrains.rider.ui.RiderSimpleToolWindowWithTwoToolbarsPanel
import com.jetbrains.rider.ui.RiderUI
import net.miginfocom.swing.MigLayout
import java.awt.BorderLayout
import java.awt.Component
import java.awt.Font
import java.awt.event.KeyAdapter
import java.awt.event.KeyEvent
import java.awt.event.MouseEvent
import java.text.SimpleDateFormat
import java.util.*
import javax.swing.*
import javax.swing.event.DocumentEvent

class UnityLogPanelView(lifetime: Lifetime, project: Project, private val logModel: UnityLogPanelModel, toolWindow: ToolWindow) {
    private val console = TextConsoleBuilderFactory.getInstance()
        .createBuilder(project)
        .filters(AnalyzeStacktraceUtil.EP_NAME.getExtensions(project))
        .console as ConsoleViewImpl

    private val tokenizer: UnityLogTokenizer = UnityLogTokenizer()

    private val eventList = UnityLogPanelEventList(lifetime).apply {
        addListSelectionListener {
            if (selectedValue != null && logModel.selectedItem != selectedValue) {
                logModel.selectedItem = selectedValue

                console.clear()
                if (selectedIndex >= 0) {
                    val date = getDateFromTicks(selectedValue.time)
                    val format = SimpleDateFormat("[HH:mm:ss:SSS] ")
                    format.timeZone = TimeZone.getDefault()
                    console.print(format.format(date), ConsoleViewContentType.NORMAL_OUTPUT)

                    val tokens = tokenizer.tokenize(selectedValue.message)
                    for (token in tokens) {
                        if (!token.used) {
                            var style = ConsoleViewContentType.NORMAL_OUTPUT.attributes

                            if (token.bold && token.italic)
                                style = TextAttributes(token.color, null, null, null, Font.BOLD or Font.ITALIC)
                            else if (token.bold)
                                style = TextAttributes(token.color, null, null, null, Font.BOLD)
                            else if (token.italic)
                                style = TextAttributes(token.color, null, null, null, Font.ITALIC)
                            else if (token.color != null)
                                style = TextAttributes(token.color, null, null, null, Font.PLAIN)

                            console.print(token.token, ConsoleViewContentType("UnityLog", style))
                        }
                    }
                    console.print("\n", ConsoleViewContentType.NORMAL_OUTPUT)
                    console.print(selectedValue.stackTrace, ConsoleViewContentType.NORMAL_OUTPUT)
                    console.scrollTo(0)
                }
            }
        }

        addKeyListener(object : KeyAdapter() {
            override fun keyPressed(e: KeyEvent?) {
                if (e?.keyCode == KeyEvent.VK_ENTER) {
                    e.consume()
                    getNavigatableForSelected(project)?.navigate(true)
                }
            }
        })

        object : DoubleClickListener() {
            override fun onDoubleClick(event: MouseEvent): Boolean {
                getNavigatableForSelected(project)?.navigate(true)
                return true
            }
        }.installOn(this)

        logModel.events.onAutoscrollChanged.advise(lifetime){
            if (it) {
                ensureIndexIsVisible(itemsCount - 1)
            }
        }
    }

    private fun getDateFromTicks(ticks: Long): Date {
        val ticksAtEpoch = 621355968000000000L
        val ticksPerMillisecond = 10000
        return Date((ticks - ticksAtEpoch) / ticksPerMillisecond)
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

    private val searchTextField = LogSmartSearchField().apply {
        focusGained = {
            eventList.clearSelection()
            logModel.selectedItem = null
        }
        goToList = {
            if (eventList.model.size > 0) {
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

    @Suppress("SpellCheckingInspection")
    private val listPanel = JPanel(MigLayout("ins 0, gap 0, flowy, novisualpadding, fill", "", "[][min!]")).apply {
        add(JBScrollPane(eventList).apply { horizontalScrollBarPolicy = ScrollPaneConstants.HORIZONTAL_SCROLLBAR_NEVER }, "grow, wmin 0")
        add(searchTextField, "grow")
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
                        addActionListener { mainSplitterOrientation.invert() }
                    })
                }.show(comp, x, y)
            }
        })
    }

    fun getMainSplitterIcon(invert: Boolean = false): Icon = when (mainSplitterOrientation.value xor invert) {
        true -> AllIcons.Actions.SplitHorizontally
        false -> AllIcons.Actions.SplitVertically
    }

    private val leftToolbar = UnityLogPanelToolbarBuilder.createLeftToolbar(logModel)

    private val topToolbar = UnityLogPanelToolbarBuilder.createTopToolbar()

    val panel = RiderSimpleToolWindowWithTwoToolbarsPanel(leftToolbar, topToolbar, mainSplitter)


    private fun removeFirstFromList() {
        if (eventList.riderModel.size() > logModel.maxItemsCount)
            eventList.riderModel.remove(0)
    }

    private fun refreshList(newEvents: List<LogPanelItem>) {
        eventList.riderModel.clear()
        eventList.riderModel.addAll(0, newEvents)

        if (logModel.selectedItem != null) {
            eventList.setSelectedValue(logModel.selectedItem, true)
        }
        else if (logModel.autoscroll.value){
            eventList.ensureIndexIsVisible(eventList.itemsCount - 1)
        }
    }

    init {
        Disposer.register(toolWindow.disposable, console)

        mainSplitterOrientation.advise(lifetime) { value ->
            mainSplitter.orientation = value
            mainSplitter.updateUI()
        }

        logModel.onChanged.advise(lifetime) { items ->
            data class LogItem(
                val type: LogEventType,
                val mode: LogEventMode,
                val message: String,
                val stackTrace: String)

            if (logModel.mergeSimilarItems.value) {
                val list = items
                    .groupBy {
                        LogItem(it.type, it.mode, it.message, it.stackTrace)
                    }
                    .mapValues { LogPanelItem(it.value.first().time, it.key.type, it.key.mode, it.key.message, it.key.stackTrace, it.value.count()) }
                    .values.toList()
                refreshList(list)
            } else {
                val list = items.map {
                    LogPanelItem(it.time, it.type, it.mode, it.message, it.stackTrace, 1)
                }
                refreshList(list)
            }
        }
        logModel.onFirstRemoved.advise(lifetime) {removeFirstFromList() }

        if (toolWindow is ToolWindowEx) {
            toolWindow.setAdditionalGearActions(DefaultActionGroup().apply {
                add(ActionManager.getInstance().getAction(RiderUnityOpenEditorLogAction.actionId))
                add(ActionManager.getInstance().getAction(RiderUnityOpenPlayerLogAction.actionId))
                add(ActionManager.getInstance().getAction(UnityPluginShowSettingsAction.actionId))

                add(Separator.getInstance())
                add(mainSplitterToggleAction)
                addAll(console.createConsoleActions().filterIsInstance<ToggleUseSoftWrapsToolbarAction>().toList())
            })
        }

        logModel.onCleared.advise(lifetime) { console.clear() }
        logModel.queueUpdate()
    }
}
