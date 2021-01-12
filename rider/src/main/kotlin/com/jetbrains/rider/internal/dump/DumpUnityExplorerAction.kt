package com.jetbrains.rider.internal.dump

import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rd.platform.internal.DumpAction
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorer
import java.io.OutputStreamWriter

class DumpUnityExplorerAction : DumpAction("Dump Unity Explorer") {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val host = UnityExplorer.getInstance(project)
        val root = host.model.root
        dump(project) { writer ->
            dumpReq(root, 0, writer)
        }
    }

    private fun dumpReq(node: AbstractTreeNode<*>, indent: Int, writer: OutputStreamWriter) {
        for (m in node.children) {
            writer.append("  ".repeat(indent))
            writer.append(m.name)
            writer.append(" ")
            writer.appendln(m.value.toString())
            if (m.children.any()) {
                if (m is AbstractTreeNode<*>)
                    dumpReq(m,indent + 1, writer)
                else
                {
                    writer.appendln(" unexpected node: " + m.toString())
                }
            }
        }
    }
}