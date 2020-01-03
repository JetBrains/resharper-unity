package unityExplorer

import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.fileEditor.ex.FileEditorManagerEx
import com.intellij.openapi.project.Project
import com.jetbrains.rider.ideaInterop.vfs.VfsWriteOperationsHost
import com.jetbrains.rider.model.RdProjectModelDumpFlags
import com.jetbrains.rider.model.RdProjectModelDumpParams
import com.jetbrains.rider.model.projectModelTasks
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorer
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorerNode
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.views.SolutionViewPaneBase
import com.jetbrains.rider.test.framework.TestProjectModelContext
import com.jetbrains.rider.test.framework.flushQueues
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.maskCacheFiles
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.util.idea.getComponent
import com.jetbrains.rider.util.idea.syncFromBackend
import java.io.File

fun TestProjectModelContext.dump(caption: String, project: Project, tempTestDirectory: File, action: () -> Unit) {

    doActionAndWait(project, action, true)
    val treeDump = dumpUnityExplorerTree(project, tempTestDirectory)

    treeOutput.appendln("===================")
    fileOutput.appendln("===================")
    treeOutput.appendln(caption)
    fileOutput.appendln(caption)
    treeOutput.appendln()
    fileOutput.appendln()
    treeOutput.appendln(treeDump)
    treeOutput.appendln()

    val dumpProjectModelTask = project.solution.projectModelTasks.dumpProjectModel
    val dumpParams = RdProjectModelDumpParams(RdProjectModelDumpFlags.Structure, hideMiscFilesProjectContent)
    val projectModelDump = dumpProjectModelTask.syncFromBackend(dumpParams, project)
    treeOutput.appendln(projectModelDump?.maskCacheFiles())
    treeOutput.appendln()

    dumpFiles(fileOutput, tempTestDirectory, false, this.profile)
}

private fun dumpUnityExplorerTree(project: Project, tempTestDirectory: File) : String {
    val tree = UnityExplorer.getInstance(project).tree
    return dumpExplorerTree(tree)
        .replace(tempTestDirectory.toPath().toUri().toString().replace("file:///", "file://"), "")
}

fun addNewItem(project: Project, path: Array<String>, template: TemplateType, itemName: String) {
    frameworkLogger.info("Start adding new item: '$itemName'")
    val dataContext = createDataContextFor2(project, path)
    changeFileSystem(project) {
        val createdFile = executeNewItemAction(dataContext, template.type, template.group!!, itemName)
        this.affectedFiles.add(createdFile!!.parentFile)
    }
    persistAllFilesOnDisk(project)
    frameworkLogger.info("New item '$itemName' is added")
}


fun createDataContextFor2(project: Project, path: Array<String>): DataContext {
    return createDataContextFor2(project, arrayOf(path))
}

fun createDataContextFor2(project: Project, paths: Array<Array<String>>): DataContext {
    flushQueues()
    val nodes = paths.map { findReq(it, project) }.toTypedArray()
    return createDataContextForNode(project, nodes)
}

fun findReq(path: Array<String>, project: Project): AbstractTreeNode<*> {
    val viewPane = UnityExplorer.getInstance(project)
    val solutionNode = viewPane.model.root
    val fileNodes = viewPane.model.root.children.filterIsInstance<UnityExplorerNode>()
    val solutionNodeName = solutionNode.name

    if (path.count() == 1) {
        if (solutionNodeName == path[0])
            return solutionNode
    } else {
        val node = findChildInternal(solutionNode, path, 1)
        if (node != null) return node
    }

    if (fileNodes.isEmpty())
        throw Exception("Node ${path.reduce { s1, s2 -> s1.split("?")[0] + "/" + s2.split("?")[0] }} not found in tree")

    val fileNode = fileNodes.find { it.name.split(SolutionViewPaneBase.TextSeparator)[0].trim() == path[0] } as? AbstractTreeNode<*>
        ?: throw Exception("Invalid name in path")

    return if (path.count() == 1)
        fileNode
    else
        findChildInternal(fileNode, path, 1)
            ?: throw Exception("Node ${path.reduce { s1, s2 -> s1.split("?")[0] + "/" + s2.split("?")[0] }} not found in tree")
}


private fun findChildInternal(node: AbstractTreeNode<*>, path: Array<String>, index: Int): AbstractTreeNode<*>? {

    val splitData = path[index].split("?")
    val requestedName = splitData[0]
    val requestedIndex = if (splitData.count() == 2) splitData[1].toInt() else 1

    var childNameIndex = 0
    for (child in node.children) {

        // Explicitly set parent for node
        // In real environment 'AbstractTreeStructureBase' do that for us immediately after getting children list
        (child as AbstractTreeNode<*>).parent = node

        val childName = child.name
        if (childName != requestedName)
            continue

        childNameIndex++
        if (childNameIndex != requestedIndex)
            continue

        val childIndex = index + 1
        if (childIndex == path.count())
            return child

        val result = findChildInternal(child, path, childIndex)
        if (result != null) return result
    }
    return null
}

private fun doActionAndWait(project: Project, action: () -> Unit, @Suppress("SameParameterValue") closeEditors: Boolean) {
    action()
    flushQueues()
    waitAllCommandsFinished()
    project.getComponent<VfsWriteOperationsHost>().waitRefreshIsFinished()

    if (closeEditors) {
        FileEditorManagerEx.getInstanceEx(project).closeAllFiles()
        flushQueues()
    }
}