package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindow
import com.intellij.util.ui.update.MergingUpdateQueue
import com.intellij.util.ui.update.Update
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.Signal
import com.jetbrains.rd.util.reactive.fire
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.model.LogEvent
import com.jetbrains.rider.plugins.unity.model.LogEventMode
import com.jetbrains.rider.plugins.unity.model.LogEventType
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

class UnityLogPanelModel(lifetime: Lifetime, val project: Project, toolWindow: ToolWindow) {
    private val lock = Object()
    val maxItemsCount = 10000

    private val mergingUpdateQueue = MergingUpdateQueue("UnityLogPanelModel->onChanged", 250, true, toolWindow.component).setRestartTimerOnAdd(false)
    private val mergingUpdateQueueAction: Update = object : Update("UnityLogPanelView->onChanged") {
        override fun run() {
            if (toolWindow.isVisible)
                onChanged.fire(getVisibleEvents())
        }
    }

    inner class TypeFilters {
        private var showErrors = true
        private var showWarnings = true
        private var showMessages = true

        fun getShouldBeShown(type: LogEventType) = when (type) {
            LogEventType.Error -> showErrors
            LogEventType.Warning -> showWarnings
            LogEventType.Message -> showMessages
        }

        fun setShouldBeShown(type: LogEventType, value: Boolean) = when (type) {
            LogEventType.Error -> {
                synchronized(lock) { showErrors = value }
                onChanged.fire()
            }
            LogEventType.Warning -> {
                synchronized(lock) { showWarnings = value }
                onChanged.fire()
            }
            LogEventType.Message -> {
                synchronized(lock) { showMessages = value }
                onChanged.fire()
            }
        }

        val onChanged = Signal.Void()
    }

    inner class ModeFilters {
        private var showEdit = true
        private var showPlay = true

        fun getShouldBeShown(mode: LogEventMode) = when (mode) {
            LogEventMode.Edit -> showEdit
            LogEventMode.Play -> showPlay
        }

        fun setShouldBeShown(mode: LogEventMode, value: Boolean) = when (mode) {
            LogEventMode.Edit -> {
                synchronized(lock) { showEdit = value }
                onChanged.fire()
            }
            LogEventMode.Play -> {
                synchronized(lock) { showPlay = value }
                onChanged.fire()
            }
        }

        val onChanged = Signal.Void()
    }

    inner class TextFilter {
        private var searchTerm = ""

        fun getShouldBeShown(text: String):Boolean {
            return text.contains(searchTerm, true)
        }

        fun setPattern(value: String) {
            synchronized(lock) { searchTerm = value }
            onChanged.fire()
        }

        val onChanged = Signal.Void()
    }

    inner class TimeFilters {
        private var showBeforePlay = true
        private var showBeforeInit = true

        fun getShouldBeShown(time: Long):Boolean {
            return (showBeforeInit || time > project.solution.frontendBackendModel.consoleLogging.lastInitTime.valueOrDefault(0))
                && (showBeforePlay || time > project.solution.frontendBackendModel.consoleLogging.lastPlayTime.valueOrDefault(0))
        }
        fun getShouldBeShownBeforeInit():Boolean {
            return showBeforeInit
        }
        fun getShouldBeShownBeforePlay():Boolean {
            return showBeforePlay
        }

        fun setShowBeforePlay(value: Boolean) {
            synchronized(lock) { showBeforePlay = value }
            onChanged.fire()
        }
        fun setShowBeforeLastBuild(value: Boolean) {
            synchronized(lock) { showBeforeInit = value }
            onChanged.fire()
        }

        val onChanged = Signal.Void()
    }

    inner class Events {
        val allEvents = ArrayList<LogEvent>()

        fun clear() {
            synchronized(lock) { allEvents.clear() }
            selectedItem = null
            onChanged.fire()
            onCleared.fire()
        }

        fun addEvent(event: LogEvent) {
            synchronized(lock) {
                if (allEvents.count() > maxItemsCount)
                {
                    onFirstRemoved.fire()
                    allEvents.removeFirst()
                }
                allEvents.add(event)
            }

            if (isVisibleEvent(event))
                onAdded.fire(event)
        }

        val onChanged = Signal.Void()

        val onAutoscrollChanged = Signal<Boolean>()
    }

    private fun isVisibleEvent(event: LogEvent):Boolean
    {
        return typeFilters.getShouldBeShown(event.type)
            && modeFilters.getShouldBeShown(event.mode)
            && textFilter.getShouldBeShown(event.message)
            && timeFilters.getShouldBeShown(event.time)
    }

    private fun getVisibleEvents(): List<LogEvent> {
        synchronized(lock) {
            return events.allEvents
                .filter { isVisibleEvent(it) }
        }
    }

    val typeFilters = TypeFilters()
    val modeFilters = ModeFilters()
    val textFilter = TextFilter()
    val events = Events()
    val mergeSimilarItems = Property(false)
    val autoscroll = Property(false)
    var timeFilters = TimeFilters()

    val onAdded = Signal<LogEvent>()
    val onFirstRemoved = Signal.Void()
    val onChanged = Signal<List<LogEvent>>()
    val onCleared = Signal.Void()

    fun fire() = mergingUpdateQueue.queue(mergingUpdateQueueAction)

    var selectedItem : LogPanelItem? = null

    init {
        typeFilters.onChanged.advise(lifetime) { fire() }
        modeFilters.onChanged.advise(lifetime) { fire() }
        textFilter.onChanged.advise(lifetime) { fire() }
        timeFilters.onChanged.advise(lifetime) { fire() }
        events.onChanged.advise(lifetime) { fire() }
        mergeSimilarItems.advise(lifetime) { fire() }
        project.solution.frontendBackendModel.consoleLogging.lastInitTime.advise(lifetime){ fire() }
        project.solution.frontendBackendModel.consoleLogging.lastPlayTime.advise(lifetime){ fire() }
    }
}