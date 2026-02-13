package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.jetbrains.rider.plugins.unity.profiler.utils.UnityProfilerFormatUtils
import javax.swing.tree.DefaultMutableTreeNode

data class UnityProfilerNodeData(
    val name: String,
    val ms: Double,
    val memory: Long,
    val isProfilerMarker: Boolean = false,
    val framePercentage: Double = 0.0
) {
    override fun toString(): String = name
    
    val memoryRepresentation: String get() = UnityProfilerFormatUtils.formatFileSize(memory, fixedFractionPrecision = true)
}

val DefaultMutableTreeNode.nodeData: UnityProfilerNodeData?
    get() = userObject as? UnityProfilerNodeData

fun DefaultMutableTreeNode.childrenNodes(): List<DefaultMutableTreeNode> =
    (0 until childCount).map { getChildAt(it) as DefaultMutableTreeNode }
