package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.openapi.Disposable
import com.intellij.openapi.application.edtWriteAction
import com.intellij.openapi.application.readAction
import com.intellij.openapi.editor.Document
import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.openapi.fileEditor.FileDocumentSynchronizationVetoer
import com.intellij.openapi.vfs.VirtualFileManager
import com.intellij.testFramework.common.timeoutRunBlocking
import com.intellij.testFramework.junit5.TestApplication
import com.intellij.testFramework.junit5.TestDisposable
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpEntry
import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpServerEntry
import com.jetbrains.rider.plugins.unity.settings.agents.readMcpServersEntry
import com.jetbrains.rider.plugins.unity.settings.agents.writeUnityMcpJsonStoreInIde
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNotNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.io.TempDir
import java.nio.file.Path
import kotlin.io.path.readText
import kotlin.io.path.writeText

/**
 * Pins what a write of a store does to an editor that shows the store.
 *
 * The edits of the user in the editor reach the file before the merge, and the editor shows the merged file after.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
@TestApplication
class UnityMcpJsonStoreInIdeTest {
    @TempDir
    lateinit var solution: Path

    @TestDisposable
    lateinit var disposable: Disposable

    private val entry = UnityMcpServerEntry("/home/tester/.unity/bin/unity", listOf("mcp"), type = "stdio")

    @Test
    fun `a configure keeps the unsaved edits of the user and the editor shows the result`() = timeoutRunBlocking {
        val file = solution.resolve(".mcp.json")
        file.writeText("""{"mcpServers": {}}""")
        val document = openDocument(file)
        edtWriteAction { document.setText(USER_EDIT) }

        assertTrue(writeUnityMcpJsonStoreInIde(file, entry))

        val onDisk = file.readText()
        assertTrue(onDisk.contains(""""idea"""")) { "the write lost the unsaved edit of the user, expected \"idea\" in: $onDisk" }
        assertEquals(UnityMcpEntry.PRESENT, readMcpServersEntry(onDisk, fileExists = true))
        readAction {
            assertEquals(onDisk, document.text, "the editor still shows the file from before the write")
            assertFalse(FileDocumentManager.getInstance().isDocumentUnsaved(document))
        }
    }

    @Test
    fun `a removal keeps the unsaved edits of the user and the editor shows the result`() = timeoutRunBlocking {
        val file = solution.resolve(".mcp.json")
        file.writeText("""{"mcpServers": {}}""")
        val document = openDocument(file)
        edtWriteAction { document.setText(USER_EDIT_WITH_UNITY) }

        assertTrue(writeUnityMcpJsonStoreInIde(file, null))

        val onDisk = file.readText()
        assertTrue(onDisk.contains(""""idea"""")) { "the removal lost the unsaved edit of the user, expected \"idea\" in: $onDisk" }
        assertEquals(UnityMcpEntry.ABSENT, readMcpServersEntry(onDisk, fileExists = true))
        readAction { assertEquals(onDisk, document.text, "the editor still shows the file from before the removal") }
    }

    @Test
    fun `a save that does not land stops the write and keeps the edits of the user`() = timeoutRunBlocking {
        val file = solution.resolve(".mcp.json")
        val original = """{"mcpServers": {}}"""
        file.writeText(original)
        val document = openDocument(file)
        edtWriteAction { document.setText(USER_EDIT) }
        FileDocumentSynchronizationVetoer.EP_NAME.point.registerExtension(RefusesEverySave(), disposable)

        assertFalse(writeUnityMcpJsonStoreInIde(file, entry), "the write went on after the save did not land")

        assertEquals(original, file.readText(), "the write changed the store under an unsaved editor")
        readAction {
            assertEquals(USER_EDIT, document.text, "the write lost the unsaved edit of the user")
            assertTrue(FileDocumentManager.getInstance().isDocumentUnsaved(document))
        }
    }

    @Test
    fun `a configure of a store that did not exist makes the file known to the IDE`() = timeoutRunBlocking {
        val file = solution.resolve(".junie").resolve("mcp").resolve("mcp.json")

        assertTrue(writeUnityMcpJsonStoreInIde(file, entry))

        assertEquals(UnityMcpEntry.PRESENT, readMcpServersEntry(file.readText(), fileExists = true))
        assertNotNull(VirtualFileManager.getInstance().findFileByNioPath(file), "the IDE does not see the new store")
    }

    private class RefusesEverySave : FileDocumentSynchronizationVetoer() {
        override fun maySaveDocument(document: Document, isSaveExplicit: Boolean): Boolean = false
    }

    private suspend fun openDocument(file: Path): Document {
        val virtualFile = checkNotNull(VirtualFileManager.getInstance().refreshAndFindFileByNioPath(file))
        return readAction { checkNotNull(FileDocumentManager.getInstance().getDocument(virtualFile)) }
    }

    private companion object {
        const val USER_EDIT = """{"mcpServers": {"idea": {"command": "idea"}}}"""
        const val USER_EDIT_WITH_UNITY =
            """{"mcpServers": {"idea": {"command": "idea"}, "unity-editor-mcp": {"command": "unity", "args": ["mcp"]}}}"""
    }
}
