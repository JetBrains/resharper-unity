package com.jetbrains.rider.plugins.unity.profiler.viewModels

import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.SequentialLifetimes
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rider.plugins.unity.model.UnityProfilerRecordInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendModelSnapshot
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.UnityProfilerSortColumn
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.UnityProfilerTreeBuilder
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.nodeData
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach
import javax.swing.SortOrder
import javax.swing.tree.DefaultMutableTreeNode

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
class UnityProfilerTreeViewModel(
    val profilerModel: FrontendBackendProfilerModel,
    val snapshotModel: UnityProfilerSnapshotModel,
    val lifetime: Lifetime
) {
    private val updateTreeLifetimes = SequentialLifetimes(lifetime)

    // Guard flag to prevent circular update loops
    private var isUpdatingFromModel = false

    // Debounced filter input to reduce rebuild frequency during typing
    private val filterInputFlow = MutableSharedFlow<Pair<String, Boolean>>(
        extraBufferCapacity = 1,
        onBufferOverflow = BufferOverflow.DROP_OLDEST
    )

    val currentProfilerRecordInfo: Property<UnityProfilerRecordInfo?> = Property(null)
    val currentSnapshot: IProperty<FrontendModelSnapshot?> get() = snapshotModel.currentSnapshot
    val filterText: Property<String> = Property("")
    val isExactFilter: Property<Boolean> = Property(false)
    val activeSortColumn: Property<UnityProfilerSortColumn> = Property(UnityProfilerSortColumn.MS)
    val activeSortOrder: Property<SortOrder> = Property(SortOrder.DESCENDING)
    val visibleColumns: Property<Set<UnityProfilerSortColumn>> = Property(UnityProfilerSortColumn.entries.toSet())
    val treeRoot: Property<DefaultMutableTreeNode?> = Property(null)

    init {
        profilerModel.currentProfilerRecordInfo.flowInto(lifetime, currentProfilerRecordInfo)

        // Set up debounced filter input (300ms delay to prevent excessive rebuilds during typing)
        filterInputFlow
            .debounce(300)
            .onEach { (text, exact) ->
                filterText.set(text)
                isExactFilter.set(exact)
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
        filterText.advise(lifetime, updateAction)
        isExactFilter.advise(lifetime, updateAction)
        activeSortColumn.advise(lifetime, updateAction)
        activeSortOrder.advise(lifetime, updateAction)
    }

    private fun updateTree() {
        val snapshot = currentSnapshot.value ?: return
        val updateLifetime = updateTreeLifetimes.next()
        if (!updateLifetime.isAlive) return

        val root = UnityProfilerTreeBuilder.buildTree(
            snapshot.samples,
            updateLifetime,
            getCurrentComparator(),
            filterText.value,
            isExactFilter.value
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
     * Sets the filter text and mode with debouncing to prevent excessive tree rebuilds.
     *
     * Filter changes are debounced (300ms delay) to reduce CPU usage during rapid typing.
     * The actual filter application happens asynchronously after the debounce period.
     *
     * @param text Filter text to match against node names
     * @param exact If true, requires exact match; if false, uses contains match (case-insensitive)
     *
     * Thread-safety: Can be called from any thread; debouncing handled by coroutine flow.
     */
    fun setFilter(text: String, exact: Boolean) {
        filterInputFlow.tryEmit(text to exact)
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
        val targetNode = findFirstNonProfilerMarkerNode(node) ?: return
        val qualifiedName = targetNode.nodeData?.name ?: return
        profilerModel.navigateByQualifiedName.fire(qualifiedName)
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