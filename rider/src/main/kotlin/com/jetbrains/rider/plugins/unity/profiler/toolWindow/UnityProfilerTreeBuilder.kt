package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.openapi.progress.ProgressManager
import com.intellij.util.ui.tree.TreeUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerModelSample
import javax.swing.tree.DefaultMutableTreeNode

/**
 * Builder for Unity Profiler tree structure with cancellation support.
 *
 * Converts flat profiler sample list into hierarchical tree for UI display.
 * Supports filtering, sorting, and node merging by qualified name.
 *
 * ## Performance Characteristics
 * - Time complexity: O(n log n) where n = node count (due to sorting)
 * - Space complexity: O(n) for tree structure
 * - Cancellation checkpoints: Every ~10-20 iterations for responsive cancellation
 *
 * ## Cancellation
 * Checks [ProgressManager.checkCanceled] and [Lifetime.isAlive] periodically to allow:
 * - Write actions to proceed (IntelliJ Platform requirement)
 * - User-initiated cancellation (e.g., changing filter rapidly)
 * - Fast response to new requests (<500ms cancellation latency)
 *
 * Thread-safety: Can be called from any thread; checks cancellation throughout.
 */
object UnityProfilerTreeBuilder {
    /**
     * Builds profiler tree from flat sample list with cancellation support.
     *
     * @param samples Flat list of profiler samples from backend (depth-first pre-order traversal)
     * @param lifetime Cancellation token; terminate to cancel tree building
     * @param initialComparator Optional comparator for sorting nodes (null = no sorting)
     * @param filterPattern Optional filter text for node names (null = no filtering)
     * @param isExactFilter If true, requires exact name match; if false, uses contains match
     * @return Root tree node, or null if cancelled during building
     *
     * Thread-safety: Can be called from any thread; checks cancellation periodically.
     * Complexity: O(n log n) where n = node count (due to sorting).
     */
    fun buildTree(
        samples: List<ProfilerModelSample>,
        lifetime: Lifetime,
        initialComparator: Comparator<DefaultMutableTreeNode>? = null,
        filterPattern: String? = null,
        isExactFilter: Boolean = false
    ): DefaultMutableTreeNode? {
        val root = DefaultMutableTreeNode()
        if (samples.isEmpty()) return root

        val iterator = samples.iterator()
        while (iterator.hasNext()) {
            // Check cancellation to allow write actions and prevent UI freezes
            ProgressManager.checkCanceled()
            if (!lifetime.isAlive) return null
            val childNode = buildNode(iterator, lifetime) ?: return null
            root.add(childNode)
        }

        if (!lifetime.isAlive) return null
        mergeNodesByQualifiedName(root)

        if (!filterPattern.isNullOrBlank()) {
            if (!filterNode(root, filterPattern, isExactFilter, lifetime)) return null
        }

        if (!lifetime.isAlive) return null
        if (initialComparator != null) {
            TreeUtil.sortRecursively(root, initialComparator)
        }

        return root
    }

    private fun buildNode(iterator: Iterator<ProfilerModelSample>, lifetime: Lifetime): DefaultMutableTreeNode? {
        if (!lifetime.isAlive || !iterator.hasNext()) return null
        val sample = iterator.next()
        val nodeData = UnityProfilerNodeData(sample.qualifiedName, sample.duration, sample.memoryAllocation, sample.isProfilerMarker, sample.framePercentage)
        val node = DefaultMutableTreeNode(nodeData)

        repeat(sample.childrenCount) {
            // Check cancellation to allow write actions during recursive tree building
            ProgressManager.checkCanceled()
            if (!lifetime.isAlive) return null
            val childNode = buildNode(iterator, lifetime) ?: return null
            node.add(childNode)
        }
        
        return node
    }

    private fun filterNode(
        node: DefaultMutableTreeNode,
        pattern: String,
        isExactFilter: Boolean,
        lifetime: Lifetime,
        keepAllChildren: Boolean = false
    ): Boolean {
        // Check cancellation periodically during filtering
        ProgressManager.checkCanceled()
        if (!lifetime.isAlive) return false

        val name = node.nodeData?.name ?: ""
        val nodeMatches = if (isExactFilter) {
            name.equals(pattern, ignoreCase = true)
        } else {
            name.contains(pattern, ignoreCase = true)
        }

        val shouldKeepAllChildren = keepAllChildren || nodeMatches

        val children = node.childrenNodes()
        var anyChildMatches = false
        for (child in children) {
            // Check cancellation periodically during filtering
            ProgressManager.checkCanceled()
            if (!lifetime.isAlive) return false
            if (filterNode(child, pattern, isExactFilter, lifetime, shouldKeepAllChildren)) {
                anyChildMatches = true
            } else {
                node.remove(child)
            }
        }

        return nodeMatches || anyChildMatches || keepAllChildren
    }

    private fun mergeNodesByQualifiedName(parent: DefaultMutableTreeNode) {
        if (parent.childCount <= 0) return

        // Check cancellation before processing large node sets
        ProgressManager.checkCanceled()

        val childrenByName = parent.childrenNodes().groupBy { it.nodeData?.name }

        // Skip if no merging needed (all children have unique names)
        if (childrenByName.values.all { it.size == 1 }) {
            // Just recursively process children in place
            for (child in parent.childrenNodes()) {
                mergeNodesByQualifiedName(child)
            }
            return
        }

        // Build merged children list without clearing parent yet
        val mergedChildren = mutableListOf<DefaultMutableTreeNode>()

        for ((name, nodes) in childrenByName) {
            // Check cancellation before processing each node group
            ProgressManager.checkCanceled()
            if (name == null) continue
            val mergedNode = if (nodes.size == 1) {
                // Reuse single node without copying
                nodes[0].also { mergeNodesByQualifiedName(it) }
            } else {
                // Merge multiple nodes with same name
                val totalMs = nodes.sumOf { it.nodeData?.ms ?: 0.0 }
                val totalMemory = nodes.sumOf { it.nodeData?.memory ?: 0L }
                val totalFramePercentage = nodes.sumOf { it.nodeData?.framePercentage ?: 0.0 }
                val merged = DefaultMutableTreeNode(UnityProfilerNodeData(name, totalMs, totalMemory, framePercentage = totalFramePercentage))

                // Collect all grandchildren from nodes being merged (optimized single pass)
                for (node in nodes) {
                    val childCount = node.childCount
                    for (i in 0 until childCount) {
                        merged.add(node.getChildAt(0) as DefaultMutableTreeNode)
                    }
                }

                mergeNodesByQualifiedName(merged)
                merged
            }
            mergedChildren.add(mergedNode)
        }

        // Only now replace parent's children with merged list
        parent.removeAllChildren()
        mergedChildren.forEach { parent.add(it) }
    }
}
