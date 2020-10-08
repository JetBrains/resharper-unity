package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.Signal
import com.jetbrains.rd.util.reactive.fire
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.model.unity.frontendBackend.EditorLogEntry
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

class UnityLogPanelModel(lifetime: Lifetime, val project: Project) {
    private val lock = Object()
    private val maxItemsCount = 10000

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
            return (showBeforeInit || time > project.solution.frontendBackendModel.lastInitTime.valueOrDefault(0))
                && (showBeforePlay || time > project.solution.frontendBackendModel.lastPlayTime.valueOrDefault(0))
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
        val allEvents = ArrayList<EditorLogEntry>()

        fun clear() {
            synchronized(lock) { allEvents.clear() }
            selectedItem = null
            onChanged.fire()
            onCleared.fire()
        }

        fun addEvent(event: EditorLogEntry) {
            synchronized(lock) {
                if (allEvents.count() > maxItemsCount)
                {
                    clear()
                }
                allEvents.add(event)
            }

            if (isVisibleEvent(event))
                onAdded.fire(event)
        }

        val onChanged = Signal.Void()

        val onAutoscrollChanged = Signal<Boolean>()
    }

    private fun isVisibleEvent(event: EditorLogEntry):Boolean
    {
        return typeFilters.getShouldBeShown(LogEventType.fromRdLogEventTypeInt(event.type))
            && modeFilters.getShouldBeShown(LogEventMode.fromRdLogEventModeInt(event.mode))
            && textFilter.getShouldBeShown(event.message)
            && timeFilters.getShouldBeShown(event.ticks)
    }

    private fun getVisibleEvents(): List<EditorLogEntry> {
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

    val onAdded = Signal<EditorLogEntry>()
    val onChanged = Signal<List<EditorLogEntry>>()
    val onCleared = Signal.Void()

    fun fire() = onChanged.fire(getVisibleEvents())

    var selectedItem : LogPanelItem? = null

    init {
        typeFilters.onChanged.advise(lifetime) { fire() }
        modeFilters.onChanged.advise(lifetime) { fire() }
        textFilter.onChanged.advise(lifetime) { fire() }
        timeFilters.onChanged.advise(lifetime) { fire() }
        events.onChanged.advise(lifetime) { fire() }
        mergeSimilarItems.advise(lifetime) { fire() }
    }
}