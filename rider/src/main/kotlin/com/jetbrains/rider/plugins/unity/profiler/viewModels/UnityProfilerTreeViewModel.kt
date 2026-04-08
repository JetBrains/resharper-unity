package com.jetbrains.rider.plugins.unity.profiler.viewModels

import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.SequentialLifetimes
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rider.plugins.unity.model.UnityProfilerRecordInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendModelSnapshot
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerNavigationRequest
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerUsagesCollector
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.FilterMatchMode
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.FilterState
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.UnityProfilerSortColumn
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.UnityProfilerTreeBuilder
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.nodeData
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.FlowPreview
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.flowOn
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach
import javax.swing.SortOrder
import javax.swing.tree.DefaultMutableTreeNode
import kotlin.time.Duration.Companion.milliseconds

/**
 * ViewModel for Unity Profiler tree view that manages profiler data display and filtering.
 *
 * ## Threading Model
 * - RD Protocol callbacks: Background thread
 * - Tree building: Pooled coroutine (cancellable via [SequentialLifetimes])
 * - UI updates: EDT via RD Property synchronization
 *
 * ## Guard Flags Pattern
 * Uses [isUpdatingFromModel] flag with try-finally to prevent circular updates when syncing
 * between frontend Property instances and backend RD Protocol properties. This prevents
 * infinite update loops when backend sends updates while processing frontend changes.
 *
 * ## Performance Optimizations
 * - **Debounced filter input**: 300ms delay reduces tree rebuilds during rapid typing
 * - **Cancellable tree building**: Uses [SequentialLifetimes] to cancel previous builds
 *
 * @property profilerModel RD Protocol model for communicating with backend
 * @property lifetime Lifecycle for subscriptions and coroutines
 */
@OptIn(FlowPreview::class)
class UnityProfilerTreeViewModel(
    val profilerModel: FrontendBackendProfilerModel,
    val snapshotModel: UnityProfilerSnapshotModel,
    private val project: Project,
    val lifetime: Lifetime
) {
    private val updateTreeLifetimes = SequentialLifetimes(lifetime)

    // Guard flag to prevent circular update loops
    private var isUpdatingFromModel = false

    // Debounced filter input to reduce rebuild frequency during typing
    private val filterInputFlow = MutableSharedFlow<String>(
        extraBufferCapacity = 1,
        onBufferOverflow = BufferOverflow.DROP_OLDEST
    )

    val currentProfilerRecordInfo: Property<UnityProfilerRecordInfo?> = Property(null)
    val currentSnapshot: IProperty<FrontendModelSnapshot?> get() = snapshotModel.currentSnapshot
    val filterState: Property<FilterState> = Property(FilterState())
    val activeSortColumn: Property<UnityProfilerSortColumn> = Property(UnityProfilerSortColumn.MS)
    val activeSortOrder: Property<SortOrder> = Property(SortOrder.DESCENDING)
    val visibleColumns: Property<Set<UnityProfilerSortColumn>> = Property(UnityProfilerSortColumn.entries.toSet())
    val treeRoot: Property<DefaultMutableTreeNode?> = Property(null)

    init {
        profilerModel.currentProfilerRecordInfo.flowInto(lifetime, currentProfilerRecordInfo)

        // Set up debounced filter input (300ms delay to prevent excessive rebuilds during typing)
        filterInputFlow
            .debounce(300.milliseconds)
            .flowOn(Dispatchers.EDT)
            .onEach { text ->
                filterState.set(FilterState(text, FilterMatchMode.CONTAINS))
            }
            .launchIn(lifetime.coroutineScope)

        val updateAction = { _: Any? ->
            if (!isUpdatingFromModel) {
                isUpdatingFromModel = true
                try {
                    updateTree()
                } finally {
                    isUpdatingFromModel = false
                }
            }
        }
        currentSnapshot.advise(lifetime, updateAction)
        filterState.advise(lifetime, updateAction)
        activeSortColumn.advise(lifetime, updateAction)
        activeSortOrder.advise(lifetime, updateAction)
    }

    private fun updateTree() {
        val snapshot = currentSnapshot.value ?: return
        val updateLifetime = updateTreeLifetimes.next()
        if (!updateLifetime.isAlive) return
        val filter = filterState.value

        val root = UnityProfilerTreeBuilder.buildTree(
            snapshot.samples,
            updateLifetime,
            getCurrentComparator(),
            filter.text,
            filter.mode
        )

        if (updateLifetime.isAlive) {
            treeRoot.set(root)
        }
    }

    private fun getCurrentComparator(): Comparator<DefaultMutableTreeNode>? {
        val column = activeSortColumn.value.column
        val comparator = column.comparator as? Comparator<DefaultMutableTreeNode> ?: return null
        return if (activeSortOrder.value == SortOrder.DESCENDING) comparator.reversed() else comparator
    }

    /**
     * Sets the filter text and mode, applying immediately.
     * Use for programmatic navigation (gutter markers, context menu "Filter by").
     *
     * @param text Filter text to match against node names
     * @param mode Filter match mode
     *
     * Thread-safety: Can be called from any thread.
     */
    fun setFilter(text: String, mode: FilterMatchMode) {
        filterState.set(FilterState(text, mode))
    }

    /**
     * Sets the filter text with debouncing (300ms) to reduce CPU usage during typing.
     * Use for filter text field input.
     *
     * @param text Filter text to match against node names
     *
     * Thread-safety: Can be called from any thread.
     */
    fun setFilterFromInput(text: String) {
        filterInputFlow.tryEmit(text)
    }

    /**
     * Changes the active sort column, toggling sort order if clicking the same column.
     *
     * First click on a column: descending order
     * Second click on same column: ascending order
     * Click on different column: descending order
     *
     * @param column The column to sort by
     */
    fun changeSort(column: UnityProfilerSortColumn) {
        if (activeSortColumn.value == column) {
            activeSortOrder.set(if (activeSortOrder.value == SortOrder.DESCENDING) SortOrder.ASCENDING else SortOrder.DESCENDING)
        } else {
            activeSortColumn.set(column)
            activeSortOrder.set(SortOrder.DESCENDING)
        }
    }

    fun toggleColumnVisibility(column: UnityProfilerSortColumn) {
        if (column == UnityProfilerSortColumn.NAME) return
        val current = visibleColumns.value
        if (current.contains(column)) {
            if (current.size > 1) {
                visibleColumns.set(current - column)
            }
        } else {
            visibleColumns.set(current + column)
        }
    }

    fun navigate(node: DefaultMutableTreeNode) {
        val nodeData = node.nodeData ?: return
        if (nodeData.isProfilerMarker) {
            val parentNode = findFirstNonProfilerMarkerNode(node) ?: return
            val parentName = parentNode.nodeData?.name ?: return
            profilerModel.navigateByQualifiedName.fire(ProfilerNavigationRequest(parentName, nodeData.name))
        } else {
            profilerModel.navigateByQualifiedName.fire(ProfilerNavigationRequest(nodeData.name, null))
        }
        UnityProfilerUsagesCollector.logNavigateTreeToCode(project)
    }

    private fun findFirstNonProfilerMarkerNode(node: DefaultMutableTreeNode): DefaultMutableTreeNode? {
        var current: DefaultMutableTreeNode? = node
        while (current != null) {
            val nodeData = current.nodeData
            if (nodeData != null && !nodeData.isProfilerMarker) {
                return current
            }
            current = current.parent as? DefaultMutableTreeNode
        }
        return null
    }
}