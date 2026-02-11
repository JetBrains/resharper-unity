package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.openapi.util.NlsContexts
import com.intellij.ui.treeStructure.treetable.TreeColumnInfo
import com.intellij.util.ui.ColumnInfo
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import java.text.Collator
import javax.swing.JLabel
import javax.swing.SwingConstants
import javax.swing.table.TableCellRenderer
import javax.swing.tree.DefaultMutableTreeNode

object UnityProfilerColumns {
    private val NAME_COLLATOR = Collator.getInstance().apply { strength = Collator.PRIMARY }

    private fun createNodeComparator(
        comparator: Comparator<UnityProfilerNodeData>
    ): Comparator<Any> = Comparator { o1, o2 ->
        val d1 = (o1 as? DefaultMutableTreeNode)?.nodeData ?: return@Comparator 0
        val d2 = (o2 as? DefaultMutableTreeNode)?.nodeData ?: return@Comparator 0
        comparator.compare(d1, d2)
    }

    private abstract class UnityProfilerColumnInfo<T>(@NlsContexts.ColumnName name: String) : ColumnInfo<Any, T>(name) {
        override fun isCellEditable(item: Any?) = false
        override fun getPreferredStringValue() = "000.00"
        override fun getMaxStringValue(): String = "000,000.00 MB"

        override fun getCustomizedRenderer(item: Any?, renderer: TableCellRenderer): TableCellRenderer {
            if (renderer is JLabel) {
                renderer.horizontalAlignment = SwingConstants.RIGHT
            }
            return renderer
        }
    }

    val nameColumn: TreeColumnInfo = object : TreeColumnInfo(UnityUIBundle.message("unity.profiler.toolwindow.column.name.name")) {
        override fun getComparator() = createNodeComparator { d1, d2 ->
            NAME_COLLATOR.compare(d1.name, d2.name).takeIf { it != 0 }
                ?: d1.ms.compareTo(d2.ms)
        }
    }

    val msColumn: ColumnInfo<Any, String> = object : UnityProfilerColumnInfo<String>(UnityUIBundle.message("unity.profiler.toolwindow.column.name.ms")) {
        override fun valueOf(item: Any?): String {
            val ms = (item as? DefaultMutableTreeNode)?.nodeData?.ms ?: 0.0
            return UnityProfilerFormatUtils.formatMs(ms)
        }
        override fun getColumnClass() = String::class.java
        override fun getComparator() = createNodeComparator { d1, d2 ->
            d1.ms.compareTo(d2.ms).takeIf { it != 0 } ?: d1.name.compareTo(d2.name)
        }
    }

    val memoryColumn: ColumnInfo<Any, String> = object : UnityProfilerColumnInfo<String>(UnityUIBundle.message("unity.profiler.toolwindow.column.name.memory")) {
        override fun valueOf(item: Any?): String = (item as? DefaultMutableTreeNode)?.nodeData?.memoryRepresentation ?: ""
        override fun getColumnClass() = String::class.java
        override fun getComparator() = createNodeComparator { d1, d2 ->
            d1.memory.compareTo(d2.memory).takeIf { it != 0 } ?: d1.name.compareTo(d2.name)
        }
    }

    val framePercentageColumn: ColumnInfo<Any, String> = object : UnityProfilerColumnInfo<String>(UnityUIBundle.message("unity.profiler.toolwindow.column.name.frame.percentage")) {
        override fun valueOf(item: Any?): String {
            val percentage = (item as? DefaultMutableTreeNode)?.nodeData?.framePercentage ?: 0.0
            return UnityProfilerFormatUtils.formatPercentage(percentage)
        }
        override fun getColumnClass() = String::class.java
        override fun getComparator() = createNodeComparator { d1, d2 ->
            d1.framePercentage.compareTo(d2.framePercentage).takeIf { it != 0 } ?: d1.name.compareTo(d2.name)
        }
    }

    val allColumns: Array<ColumnInfo<in Any, out Any?>> = arrayOf(nameColumn, msColumn, memoryColumn, framePercentageColumn)
}
