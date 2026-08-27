package com.jetbrains.rider.unity.test.framework.api

import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.fileEditor.ex.FileEditorManagerEx
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.RdDndOrderType
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.ideaInterop.vfs.VfsWriteOperationsHost
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorer
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorerFileSystemNode
import com.jetbrains.rider.projectView.ProjectVirtualFileView
import com.jetbrains.rider.projectView.actions.newFile.RiderNewDirectoryAction
import com.jetbrains.rider.projectView.getOrCreateActualElement
import com.jetbrains.rider.projectView.getProjectElementView
import com.jetbrains.rider.projectView.moveProviders.RiderCutProvider
import com.jetbrains.rider.projectView.moveProviders.RiderDeleteProvider
import com.jetbrains.rider.projectView.moveProviders.RiderPasteProvider
import com.jetbrains.rider.projectView.moveProviders.impl.DuplicateNameDialog
import com.jetbrains.rider.projectView.nodes.getVirtualFile
import com.jetbrains.rider.projectView.views.SolutionViewPaneBase
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerViewPane
import com.jetbrains.rider.protocol.protocolHost
import com.jetbrains.rider.test.facades.projectmodel.ProjectModelDumpApiFacade
import com.jetbrains.rider.test.facades.projectmodel.RiderProjectModelApiFacade
import com.jetbrains.rider.test.facades.projectmodel.RiderProjectModelDumpApiFacade
import com.jetbrains.rider.test.facades.solution.SolutionApiFacade
import com.jetbrains.rider.test.framework.flushQueues
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.framework.testData.TestDataStorage
import com.jetbrains.rider.test.maskCacheFiles
import com.jetbrains.rider.test.scriptingApi.TemplateType
import com.jetbrains.rider.test.scriptingApi.changeFileSystem
import com.jetbrains.rider.test.scriptingApi.createDataContextForTree
import com.jetbrains.rider.test.scriptingApi.dumpFilteredTree
import com.jetbrains.rider.test.scriptingApi.executeNewItemAction
import com.jetbrains.rider.test.scriptingApi.findChildInternal
import com.jetbrains.rider.test.scriptingApi.renameItem
import com.jetbrains.rider.test.scriptingApi.testProjectModel
import com.jetbrains.rider.test.scriptingApi.waitAllCommandsFinished
import com.jetbrains.rider.test.scriptingApi.waitForProjectModelReady
import com.jetbrains.rider.test.scriptingApi.waitForWorkspaceModelReady
import com.jetbrains.rider.test.scriptingApi.waitRefreshIsFinished
import javax.swing.JTree

class UnityProjectModelDumpApiFacade(solutionApiFacade: SolutionApiFacade, testDataStorage: TestDataStorage) : RiderProjectModelDumpApiFacade(solutionApiFacade, testDataStorage) {
    override fun dumpSolutionExplorerTree(caption: String, targetPath: List<String>, action: () -> Unit) {
        val project = solutionApiFacade.project
        val tempTestDirectory = solutionApiFacade.activeSolutionDirectory
        val tree = UnityExplorer.getInstance(project).tree
        val dumpResult = dumpExplorerTree(project, tree)
            .replace(tempTestDirectory.toUri().toString(), "")
            .replace(tempTestDirectory.toUri().toString().replace("file:///", "file://"), "")

        dump.treeOutput.appendLine("===================")
        dump.treeOutput.appendLine(caption)
        dump.treeOutput.appendLine()
        dump.treeOutput.appendLine(dumpResult)
    }

    private fun dumpExplorerTree(project: Project, tree: JTree) : String {
        val dump = dumpFilteredTree(project, tree)
        return dump
            .replace(" Scratches and Consoles", "")
            .replace(SolutionViewPaneBase.TextSeparator, "*")
            .replace(" * no index", "")
            .maskCacheFiles()
            .replace("""(\s+)-Plugins$(\1\s+\S+$)*""".toRegex(RegexOption.MULTILINE), "") + "\n"
    }
}

class UnityProjectModelApiFacade(solutionApiFacade: SolutionApiFacade, testDataStorage: TestDataStorage) : RiderProjectModelApiFacade(solutionApiFacade, testDataStorage) {
    override val projectModelDumpApiFacade: RiderProjectModelDumpApiFacade = UnityProjectModelDumpApiFacade(solutionApiFacade, testDataStorage)
}

context(solutionApiFacade: SolutionApiFacade, testDataStorage: TestDataStorage)
fun testUnityProjectModel(profileModifier: ProjectModelDumpApiFacade.DumpProfile.() -> Unit = {}, action: ProjectModelDumpApiFacade.() -> Unit) {
    val unityProjectModelApiFacade = UnityProjectModelApiFacade(solutionApiFacade, testDataStorage)
    testProjectModel(unityProjectModelApiFacade, profileModifier, action)
}

fun addNewItem2(project: Project, path: Array<String>, template: TemplateType, itemName: String) {
    frameworkLogger.info("Start adding new item: '$itemName'")
    val dataContext = createDataContextForUnityExplorer(project, arrayOf(path))
    changeFileSystem(project) {
        val createdFile = executeNewItemAction(dataContext, template.type, template.group!!, itemName)
        this.affectedFiles.add(createdFile.parent)
    }
    persistAllFilesOnDisk()
    frameworkLogger.info("New item '$itemName' is added")
}

fun addNewFolder2(project: Project, path: Array<String>, folderName: String) {
    frameworkLogger.info("Start adding new item: '$folderName'")
    val dataContext = createDataContextForUnityExplorer(project, arrayOf(path))
    addNewFolder2(project, dataContext, folderName)
}

private fun addNewFolder2(project: Project,
                         dataContext: DataContext,
                         folderName: String) {
    changeFileSystem(project) {
        val createdFile = RiderNewDirectoryAction().execute(dataContext, folderName)
        this.affectedFiles.add(createdFile!!.toNioPath())
    }
    persistAllFilesOnDisk()
    frameworkLogger.info("New folder '$folderName' is added")
}

fun renameItem(project: Project, path: Array<String>, newName: String) {
    val dataContext = createDataContextForUnityExplorer(project, arrayOf(path))
    renameItem(project, dataContext, newName, false)
}

fun deleteElement(project: Project, path: Array<String>) {
    val dataContext = createDataContextForUnityExplorer(project, arrayOf(path))
    assert(RiderDeleteProvider.canDeleteElement(dataContext)) { "Can't delete elements" }
    RiderDeleteProvider.deleteElement(dataContext)
    waitForProjectModelReady(project)
}

fun createDataContextForUnityExplorer(project: Project, paths: Array<Array<String>>): DataContext {
    val unityExplorer = UnityExplorer.getInstance(project)
    return createDataContextForTree(project, unityExplorer, paths)
}

fun findReq(path: Array<String>, project: Project): AbstractTreeNode<*> {
    val viewPane = UnityExplorer.getInstance(project)
    val solutionNode = viewPane.model.root
    val fileNodes = viewPane.model.root.children.filterIsInstance<UnityExplorerFileSystemNode>()
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

fun doActionAndWait(project: Project, action: () -> Unit, @Suppress("SameParameterValue") closeEditors: Boolean) {
    action()
    flushQueues(project.protocolHost)
    waitAllCommandsFinished()
    VfsWriteOperationsHost.getInstance(project).waitRefreshIsFinished()

    if (closeEditors) {
        FileEditorManagerEx.getInstanceEx(project).closeAllFiles()
        flushQueues(project.protocolHost)
    }
}

fun cutItem2(project: Project, path: Array<String>) {
    cutItem2(project, arrayOf(path))
}

fun cutItem2(project: Project, paths: Array<Array<String>>) {
    val dataContext = createDataContextForUnityExplorer(project, paths)
    assert(RiderCutProvider.isCutEnabled(dataContext)) { "Can't cut elements. isCutEnabled" }
    assert(RiderCutProvider.isCutVisible(dataContext)) { "Can't cut elements. isCutVisible" }
    RiderCutProvider.performCut(dataContext)
}

fun pasteItem2(project: Project, path: Array<String>, customName: String? = null, orderType : RdDndOrderType? = null) {
    val dataContext = createDataContextForUnityExplorer(project, arrayOf(path))
    assert(RiderPasteProvider.isPasteEnabled(dataContext)) { "Can't paste elements. isPasteEnabled" }
    assert(RiderPasteProvider.isPastePossible(dataContext)) { "Can't paste elements. isPastePossible" }
    Lifetime.using { lifetime ->
        DuplicateNameDialog.withCustomName(lifetime, customName)
        if (orderType != null) {
            val element = dataContext.getProjectElementView()
                ?: dataContext.getVirtualFile()?.let { ProjectVirtualFileView(project, it).getOrCreateActualElement() }
                ?: return
            RiderPasteProvider.performPaste(element, dataContext, orderType)
        }
        else {
            RiderPasteProvider.performPaste(dataContext)
        }
    }
    waitForWorkspaceModelReady(project)
}

fun withUnityExplorerPane(
    project: Project,
    showTildeFolders:Boolean = true,
    showAllFiles: Boolean = false,
    action: () -> Unit) {
    val unityExplorer = UnityExplorer.getInstance(project)
    unityExplorer.showTildeFolders = showTildeFolders

    val pane = SolutionExplorerViewPane.getInstance(project)
    pane.myShowAllFiles = showAllFiles
    unityExplorer.updateFromRoot()
    try {
        action()
    } finally {
        if (!project.isDisposed) {
            pane.myShowAllFiles = false
            unityExplorer.showTildeFolders = true
            unityExplorer.updateFromRoot()
        }
    }
}
